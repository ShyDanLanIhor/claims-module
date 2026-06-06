// Production environment. The API is expected to be reachable at the same origin behind /api
// (e.g. a reverse proxy or Azure Static Web App linked backend). Override for your deployment.
export const environment = {
  production: true,
  apiBaseUrl: '/api',
};
