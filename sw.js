/* Service worker for the Tournament Tracker web app.

   It exists for one reason: phones only allow a web page to show a
   notification through a service worker (`new Notification()` throws on
   Android Chrome and on iPhone home-screen apps). It deliberately does no
   caching, so the site is always the latest version. */

self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", e => e.waitUntil(self.clients.claim()));

/* Tapping a notification brings the app forward, or opens the link it carries. */
self.addEventListener("notificationclick", e => {
  e.notification.close();
  const url = (e.notification.data && e.notification.data.url) || "./";
  e.waitUntil((async () => {
    const wins = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
    const same = wins.find(w => w.url.split("#")[0] === url.split("#")[0]);
    if (same) return same.focus();
    return self.clients.openWindow(url);
  })());
});
