import test from "node:test";
import assert from "node:assert/strict";
import { pickBdtTreasureImage, requestBdtTreasureImagePermission } from "../src/permissions/bdtTreasureImagePicker.js";

test("requestBdtTreasureImagePermission grants access when media library or camera is granted", async () => {
  const permission = await requestBdtTreasureImagePermission(async () => ({
    requestMediaLibraryPermissionsAsync: async () => ({ status: "denied" }),
    requestCameraPermissionsAsync: async () => ({ status: "granted" }),
  }));

  assert.deepEqual(permission, { granted: true, unavailable: false });
});

test("requestBdtTreasureImagePermission maps denied and unavailable states", async () => {
  const denied = await requestBdtTreasureImagePermission(async () => ({
    requestMediaLibraryPermissionsAsync: async () => ({ status: "denied" }),
    requestCameraPermissionsAsync: async () => ({ status: "denied" }),
  }));
  const unavailable = await requestBdtTreasureImagePermission(async () => {
    throw new Error("missing module");
  });

  assert.deepEqual(denied, { granted: false, unavailable: false });
  assert.deepEqual(unavailable, { granted: false, unavailable: true });
});

test("pickBdtTreasureImage maps selected library asset to upload image", async () => {
  const result = await pickBdtTreasureImage(async () => ({
    MediaTypeOptions: { Images: "Images" },
    launchImageLibraryAsync: async () => ({
      canceled: false,
      assets: [
        {
          uri: "file:///tmp/tesoro.png",
          fileName: "tesoro.png",
          mimeType: "image/png",
          fileSize: 123,
        },
      ],
    }),
  }));

  assert.deepEqual(result, {
    image: {
      uri: "file:///tmp/tesoro.png",
      name: "tesoro.png",
      type: "image/png",
      size: 123,
    },
  });
});

test("pickBdtTreasureImage supports camera source and cancellation", async () => {
  let cameraCalled = false;
  const cameraResult = await pickBdtTreasureImage(async () => ({
    launchCameraAsync: async () => {
      cameraCalled = true;
      return {
        canceled: false,
        assets: [{ uri: "file:///tmp/camera.jpg", mimeType: "image/jpeg" }],
      };
    },
    launchImageLibraryAsync: async () => ({ canceled: true }),
  }), "camera");
  const cancelled = await pickBdtTreasureImage(async () => ({
    launchImageLibraryAsync: async () => ({ canceled: true }),
  }));

  assert.equal(cameraCalled, true);
  assert.equal(cameraResult.image.type, "image/jpeg");
  assert.equal(cameraResult.image.name, "camera.jpg");
  assert.deepEqual(cancelled, { cancelled: true });
});
