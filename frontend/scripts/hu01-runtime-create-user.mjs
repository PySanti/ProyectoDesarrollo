import { chromium } from 'playwright';

const baseUrl = 'http://localhost:5173';
const username = process.env.HU01_USER ?? 'admin';
const password = process.env.HU01_PASS ?? 'admin';
const runId = Date.now().toString().slice(-8);
const userName = `HU01 Runtime ${runId}`;
const email = `hu01.runtime.${runId}@test.com`;

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext();
const page = await context.newPage();

async function loginAndOpenForm() {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForSelector('input[name="username"], input#username, input#usernameOrEmail', { timeout: 60000 });
  await page.fill('input[name="username"], input#username, input#usernameOrEmail', username);
  await page.fill('input[name="password"], input#password', password);
  await page.locator('button[type="submit"], input[type="submit"]').first().click();
  await page.waitForLoadState('networkidle', { timeout: 60000 });
  await page.waitForSelector('form', { timeout: 60000 });
}

try {
  await loginAndOpenForm();

  await page.fill('#name', userName);
  await page.fill('#email', email);
  await page.selectOption('#initialRole', 'Participante');
  await page.click('button:has-text("Crear usuario")');

  await page.waitForTimeout(4000);
  const hasSuccess = await page.locator('[data-testid="create-success"]').count();
  const hasAlert = await page.locator('[role="alert"]').count();
  if (hasSuccess > 0) {
    const successMessage = await page.locator('[data-testid="create-success"]').innerText();
    console.log(`RESULT_201=${successMessage}`);
  } else {
    const alertText = hasAlert > 0 ? await page.locator('[role="alert"]').innerText() : 'NO_ALERT';
    console.log(`RESULT_201_FAILED=${alertText}`);
    console.log(`BODY_AFTER_FIRST_SUBMIT=${(await page.locator('body').innerText()).slice(0, 1000)}`);
    throw new Error('First create-user attempt did not produce success message.');
  }

  await page.fill('#name', `${userName} Duplicate`);
  await page.fill('#email', email);
  await page.selectOption('#initialRole', 'Participante');
  await page.click('button:has-text("Crear usuario")');

  await page.waitForTimeout(3000);
  const errorMessage = await page.locator('[role="alert"]').innerText();
  console.log(`RESULT_409=${errorMessage}`);
  console.log(`RUNTIME_EMAIL=${email}`);

  await page.screenshot({ path: 'runtime-hu01-create-user.png', fullPage: true });
} finally {
  await browser.close();
}
