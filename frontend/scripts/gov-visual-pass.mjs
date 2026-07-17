// Visual-pass harness for the Gobernanza surface (SP-5c) + role-change modal.
// Headless login as admin/admin123, navigate, screenshot at desktop + mobile widths.
// Output dir via SHOT_DIR env (absolute). Read-only against the running app.
import { chromium } from 'playwright';

const baseUrl = process.env.APP_URL ?? 'http://localhost:5173';
const username = process.env.GOV_USER ?? 'admin';
const password = process.env.GOV_PASS ?? 'admin123';
const shotDir = process.env.SHOT_DIR ?? '.';

// Playwright's bundled headless-shell build can mismatch the cache; use system chrome.
const browser = await chromium.launch({ headless: true, channel: 'chrome' });
const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await context.newPage();
const log = (m) => console.log(m);

async function login() {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  // OBS-03: branded LoginScreen shows an "Iniciar sesión" button before Keycloak.
  const kcUser = 'input[name="username"], input#username, input#usernameOrEmail';
  const appeared = await page.waitForSelector(kcUser, { timeout: 4000 }).catch(() => null);
  if (!appeared) {
    const btn = page.getByRole('button', { name: /iniciar sesi/i });
    await btn.first().click({ timeout: 10000 });
    await page.waitForSelector(kcUser, { timeout: 60000 });
  }
  await page.fill(kcUser, username);
  await page.fill('input[name="password"], input#password', password);
  await page.locator('button[type="submit"], input[type="submit"]').first().click();
  await page.waitForLoadState('networkidle', { timeout: 60000 });
}

async function shot(name) {
  const path = `${shotDir}/${name}.png`;
  await page.screenshot({ path, fullPage: true });
  log(`SHOT ${path}`);
}

try {
  await login();
  // Admin lands on /identidad/usuarios; navigate via SPA links (a full page.goto
  // reloads the SPA and drops the in-memory Keycloak token -> LoginScreen).
  await page.getByRole('link', { name: /gobernanza/i }).waitFor({ timeout: 60000 });
  log('LOGGED_IN');

  // --- Gobernanza matrix ---
  await page.getByRole('link', { name: /gobernanza/i }).click();
  await page.waitForSelector('[data-testid^="gov-card-"], [data-testid="gov-load-error"]', { timeout: 30000 });
  const cards = await page.locator('[data-testid^="gov-card-"]').count();
  const loadErr = await page.locator('[data-testid="gov-load-error"]').count();
  log(`GOV cards=${cards} loadError=${loadErr}`);
  await shot('gov-desktop');

  // mobile width
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(400);
  await shot('gov-mobile');
  await page.setViewportSize({ width: 1280, height: 900 });

  // --- Role-change modal (in User Management) ---
  await page.getByRole('link', { name: /gesti.n de usuarios/i }).click();
  await page.waitForTimeout(1200);
  await shot('users-desktop');
  const openBtn = page.locator('[data-testid^="role-change-open-"]:not([disabled])').first();
  const hasOpen = await openBtn.count();
  log(`ROLE_CHANGE open-buttons(enabled)=${hasOpen}`);
  if (hasOpen > 0) {
    await openBtn.click();
    await page.waitForSelector('[data-testid="role-change-modal"]', { timeout: 10000 });
    await shot('role-modal-idle');
    // pick Administrador to trigger the irreversible warning
    const opts = await page.locator('[data-testid="role-change-select"] option').allInnerTexts();
    log(`ROLE_OPTIONS=${JSON.stringify(opts)}`);
    if (opts.some((o) => /administrador/i.test(o))) {
      await page.selectOption('[data-testid="role-change-select"]', 'Administrador');
      await page.waitForSelector('[data-testid="role-change-warning"]', { timeout: 5000 });
      await shot('role-modal-admin-warning');
    }
  } else {
    log('ROLE_CHANGE no enabled open button (no non-admin users) — skipping modal shots');
  }
  log('DONE');
} catch (e) {
  log(`ERROR ${e.message}`);
  await shot('error-state');
  process.exitCode = 1;
} finally {
  await browser.close();
}
