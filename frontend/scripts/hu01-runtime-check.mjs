import { chromium } from 'playwright';

const baseUrl = 'http://localhost:5173';
const username = process.env.HU01_USER ?? 'admin';
const password = process.env.HU01_PASS ?? 'admin123';

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext();
const page = await context.newPage();

try {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });

  const title = await page.title();
  console.log(`TITLE=${title}`);
  const bodyPreview = (await page.locator('body').innerText()).slice(0, 500);
  console.log(`BODY_PREVIEW=${bodyPreview}`);

  await page.waitForSelector('input[name="username"], input#username, input#usernameOrEmail', { timeout: 60000 });
  await page.fill('input[name="username"], input#username, input#usernameOrEmail', username);
  await page.fill('input[name="password"], input#password', password);

  const loginButton = page.locator('button[type="submit"], input[type="submit"]');
  await loginButton.first().click();

  await page.waitForLoadState('networkidle', { timeout: 60000 });
  await page.waitForTimeout(2000);

  const bodyText = await page.locator('body').innerText();
  if (bodyText.includes('No autorizado')) {
    console.log('RESULT=FORBIDDEN_UI');
    process.exitCode = 10;
  } else if (bodyText.includes('Crear usuario') || bodyText.includes('Nombre') || bodyText.includes('Correo')) {
    console.log('RESULT=FORM_VISIBLE');
  } else {
    console.log('RESULT=UNKNOWN_STATE');
    console.log(bodyText.slice(0, 1000));
    process.exitCode = 11;
  }

  await page.screenshot({ path: 'runtime-hu01-after-login.png', fullPage: true });
} finally {
  await browser.close();
}
