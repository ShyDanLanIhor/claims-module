// Dev-server proxy for /api.
// When launched by the Aspire AppHost, WithReference(api) injects the API address as the
// `services__claims-api__http__0` (or https) environment variable. Standalone `ng serve` falls back
// to the API's default local URL.
const target =
  process.env['services__claims-api__https__0'] ||
  process.env['services__claims-api__http__0'] ||
  'http://localhost:5131';

module.exports = {
  '/api': {
    target,
    secure: false,
    changeOrigin: true,
  },
};
