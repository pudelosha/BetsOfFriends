self.addEventListener('push', event => {
  let payload = {};

  if (event.data) {
    try {
      payload = event.data.json();
    } catch {
      payload = {
        title: 'Bets of Friends',
        body: event.data.text()
      };
    }
  }

  const title = payload.title || 'Bets of Friends';
  const options = {
    body: payload.body || '',
    icon: payload.icon || '/assets/icon/favicon.png',
    badge: payload.badge || '/assets/icon/favicon.png',
    data: {
      route: payload.route || '/'
    }
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', event => {
  event.notification.close();

  const route = event.notification.data && event.notification.data.route
    ? event.notification.data.route
    : '/';
  const targetUrl = new URL(route, self.location.origin).href;

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true })
      .then(clientList => {
        for (const client of clientList) {
          if ('navigate' in client && 'focus' in client) {
            return client.navigate(targetUrl).then(() => client.focus());
          }
        }

        return clients.openWindow(targetUrl);
      })
  );
});
