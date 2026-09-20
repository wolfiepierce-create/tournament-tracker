-- Tournament Tracker — database setup, part 2
-- Run this after schema.sql: Supabase → SQL Editor → New query → Run.
-- Safe to run more than once.
--
-- Two jobs:
--   1. Stop dates of birth being readable by other signed-in users.
--   2. Carry the display name and the private phone-alert channel on the post
--      or invite itself, so they are only visible to people already allowed to
--      see that row — nobody can scrape a list of alert channels.

-- ---------------------------------------------------------------- profiles

-- Previously any signed-in user could read every profile row, date of birth
-- included. Now you can only read your own; display names travel on the posts.
drop policy if exists profiles_read on public.profiles;
drop policy if exists profiles_read_own on public.profiles;
create policy profiles_read_own on public.profiles
  for select to authenticated using (id = auth.uid());

-- ---------------------------------------------------------------- posts

alter table public.posts add column if not exists author_name text;
alter table public.posts add column if not exists alert_topic text;

-- ---------------------------------------------------------------- invites

alter table public.invites add column if not exists from_name   text;
alter table public.invites add column if not exists alert_topic text;

-- Keep updated_at honest, so "what changed since I last looked" works.
create or replace function public.touch_updated_at()
returns trigger language plpgsql as $$
begin new.updated_at = now(); return new; end;
$$;

drop trigger if exists invites_touch on public.invites;
create trigger invites_touch before update on public.invites
  for each row execute function public.touch_updated_at();

-- ---------------------------------------------------------------- reports

-- Reports should capture what was said at the time, since the post may be gone
-- by the time you read it.
alter table public.reports add column if not exists details jsonb;
