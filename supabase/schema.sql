-- Tournament Tracker — database setup
-- Paste the whole file into Supabase → SQL Editor → New query → Run.
-- Safe to run more than once.
--
-- What it creates:
--   profiles   one row per account: username, display name, date of birth
--   user_data  your settings/saved tournaments, so they follow you between devices
--   posts      Matchmaking "want to play" posts (18+ only)
--   invites    invitations between two players
--   blocks     people you've hidden
--   reports    what users report, for you to read in the dashboard
--
-- Every table has Row Level Security on, so the public key in the app can only
-- ever read and write what the signed-in person is allowed to touch.

create extension if not exists citext;

-- ---------------------------------------------------------------- profiles

create table if not exists public.profiles (
  id           uuid primary key references auth.users on delete cascade,
  username     citext not null unique
               check (length(username) between 3 and 20 and username ~ '^[A-Za-z0-9_.-]+$'),
  display_name text not null check (length(display_name) between 1 and 24),
  dob          date not null,
  alert_topic  text,                       -- private ntfy channel for phone alerts
  created_at   timestamptz not null default now()
);

alter table public.profiles enable row level security;

-- Anyone signed in can look up the username behind a post; nothing else is exposed
-- by the app's queries. Dates of birth are never selected by the client.
drop policy if exists profiles_read on public.profiles;
create policy profiles_read on public.profiles
  for select to authenticated using (true);

drop policy if exists profiles_write_own on public.profiles;
create policy profiles_write_own on public.profiles
  for insert to authenticated with check (id = auth.uid());

drop policy if exists profiles_update_own on public.profiles;
create policy profiles_update_own on public.profiles
  for update to authenticated using (id = auth.uid()) with check (id = auth.uid());

-- Age is worked out from the date of birth every time it is asked, so nobody
-- ages past the gate without the app noticing.
create or replace function public.is_adult()
returns boolean language sql stable security definer set search_path = public as $$
  select exists (
    select 1 from public.profiles
    where id = auth.uid() and dob <= (current_date - interval '18 years')
  );
$$;

-- ---------------------------------------------------------------- settings sync

create table if not exists public.user_data (
  user_id    uuid primary key references auth.users on delete cascade,
  data       jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now()
);

alter table public.user_data enable row level security;

drop policy if exists user_data_own on public.user_data;
create policy user_data_own on public.user_data
  for all to authenticated using (user_id = auth.uid()) with check (user_id = auth.uid());

-- ---------------------------------------------------------------- blocks

create table if not exists public.blocks (
  blocker    uuid not null references auth.users on delete cascade,
  blocked    uuid not null references auth.users on delete cascade,
  created_at timestamptz not null default now(),
  primary key (blocker, blocked)
);

alter table public.blocks enable row level security;

drop policy if exists blocks_own on public.blocks;
create policy blocks_own on public.blocks
  for all to authenticated using (blocker = auth.uid()) with check (blocker = auth.uid());

-- ---------------------------------------------------------------- posts

create table if not exists public.posts (
  id         uuid primary key default gen_random_uuid(),
  author     uuid not null references auth.users on delete cascade,
  skill      text check (length(skill) <= 24),
  format     text check (format in ('singles','doubles','either')),
  when_key   text check (when_key in ('today','tomorrow','weekend','evenings','flexible','at')),
  play_time  timestamptz,
  note       text check (length(note) <= 140),
  court      jsonb not null,
  lat        double precision not null check (lat between -90 and 90),
  lon        double precision not null check (lon between -180 and 180),
  place      text check (length(place) <= 40),
  expires_at timestamptz not null,
  created_at timestamptz not null default now()
);

create index if not exists posts_live_idx on public.posts (expires_at);
alter table public.posts enable row level security;

-- Only adults see the board, and only adults can post. Blocked people disappear.
drop policy if exists posts_read on public.posts;
create policy posts_read on public.posts
  for select to authenticated using (
    public.is_adult()
    and expires_at > now()
    and not exists (select 1 from public.blocks b where b.blocker = auth.uid() and b.blocked = posts.author)
  );

drop policy if exists posts_write_own on public.posts;
create policy posts_write_own on public.posts
  for insert to authenticated with check (author = auth.uid() and public.is_adult());

drop policy if exists posts_delete_own on public.posts;
create policy posts_delete_own on public.posts
  for delete to authenticated using (author = auth.uid());

drop policy if exists posts_update_own on public.posts;
create policy posts_update_own on public.posts
  for update to authenticated using (author = auth.uid()) with check (author = auth.uid());

-- ---------------------------------------------------------------- invites

create table if not exists public.invites (
  id         uuid primary key default gen_random_uuid(),
  post_id    uuid not null references public.posts on delete cascade,
  from_user  uuid not null references auth.users on delete cascade,
  to_user    uuid not null references auth.users on delete cascade,
  court      jsonb not null,
  play_time  timestamptz not null,
  status     text not null default 'new' check (status in ('new','accepted','declined','cancelled')),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists invites_to_idx   on public.invites (to_user, status);
create index if not exists invites_from_idx on public.invites (from_user, status);
alter table public.invites enable row level security;

-- Only the two people involved can see or change an invitation.
drop policy if exists invites_read on public.invites;
create policy invites_read on public.invites
  for select to authenticated using (auth.uid() in (from_user, to_user));

drop policy if exists invites_send on public.invites;
create policy invites_send on public.invites
  for insert to authenticated with check (from_user = auth.uid() and public.is_adult());

drop policy if exists invites_answer on public.invites;
create policy invites_answer on public.invites
  for update to authenticated using (auth.uid() in (from_user, to_user))
  with check (auth.uid() in (from_user, to_user));

-- ---------------------------------------------------------------- reports

create table if not exists public.reports (
  id            uuid primary key default gen_random_uuid(),
  reporter      uuid not null references auth.users on delete cascade,
  reported_user uuid references auth.users on delete set null,
  post_id       uuid,
  reason        text not null check (length(reason) <= 500),
  created_at    timestamptz not null default now()
);

alter table public.reports enable row level security;

-- Users can file a report but cannot read anyone's reports, including their own.
-- You read them in the dashboard: Table Editor → reports.
drop policy if exists reports_file on public.reports;
create policy reports_file on public.reports
  for insert to authenticated with check (reporter = auth.uid());

-- ---------------------------------------------------------------- housekeeping

-- Expired posts are hidden by the read policy above; this clears them out for good.
create or replace function public.purge_expired_posts()
returns void language sql security definer set search_path = public as $$
  delete from public.posts where expires_at < now() - interval '2 days';
$$;
