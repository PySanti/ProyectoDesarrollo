const { spawn } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const args = process.argv.slice(2);
const logsDir = path.resolve(__dirname, "..", "logs");
fs.mkdirSync(logsDir, { recursive: true });

const timestamp = new Date()
  .toISOString()
  .replace(/[:.]/g, "-");
const logPath = path.join(logsDir, `mobile-expo-${timestamp}.log`);
const logStream = fs.createWriteStream(logPath, { flags: "a" });

const command = process.platform === "win32" ? "npx.cmd" : "npx";
const expoArgs = ["expo", "start", "--clear", ...args];

console.log(`Starting Expo: ${command} ${expoArgs.join(" ")}`);
console.log(`Logging Expo output to ${logPath}`);

const child = spawn(command, expoArgs, {
  cwd: path.resolve(__dirname, ".."),
  env: process.env,
  shell: false
});

child.stdout.on("data", (chunk) => {
  process.stdout.write(chunk);
  logStream.write(chunk);
});

child.stderr.on("data", (chunk) => {
  process.stderr.write(chunk);
  logStream.write(chunk);
});

child.on("error", (error) => {
  const message = `Expo start failed: ${error.message}\n`;
  process.stderr.write(message);
  logStream.write(message);
  logStream.end();
  process.exitCode = 1;
});

child.on("close", (code) => {
  const message = `\nExpo process exited with code ${code}. Log: ${logPath}\n`;
  process.stdout.write(message);
  logStream.write(message);
  logStream.end();
  process.exitCode = code ?? 0;
});
