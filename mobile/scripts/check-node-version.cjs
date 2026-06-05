const [major = 0, minor = 0, patch = 0] = process.versions.node
  .split(".")
  .map((part) => Number.parseInt(part, 10));

const isSupported = major > 20 || (major === 20 && (minor > 19 || (minor === 19 && patch >= 4)));

if (!isSupported) {
  console.error(
    [
      "Expo SDK 54 / React Native 0.81 requires Node.js 20.19.4 or newer.",
      `Current Node.js version: ${process.version}`,
      "Install/use Node 20+ before running the mobile app.",
      "Example with nvm: nvm install 20.19.4 && nvm use 20.19.4"
    ].join("\n")
  );
  process.exit(1);
}
