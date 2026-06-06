// Development environment. API calls go to the relative '/api' and are forwarded by the dev-server
// proxy (proxy.conf.js) — to the Aspire-injected API address when run via the AppHost, or to
// http://localhost:5131 when running `ng serve` standalone.
export const environment = {
  production: false,
  apiBaseUrl: '/api',
};
