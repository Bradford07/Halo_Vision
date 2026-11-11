import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, apiRequest, emptyProfile, toProfileInput } from "./api";

// Unit tests for the web app's API helpers (src/api.ts).
// Run from CompanionApp/Web with: npm test

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("emptyProfile", () => {
  // A new profile on the web app must start with the same defaults as
  // AthleteProfile.cs in HaloVision.Shared, or the phone and headset disagree.
  it("matches the defaults in the shared C# AthleteProfile", () => {
    const profile = emptyProfile();

    expect(profile.visionLossType).toBe("Peripheral");
    expect(profile.severity).toBe("Mild");
    expect(profile.affectedEye).toBe("Both");
    expect(profile.height).toBe(1.6);
    expect(profile.skillLevel).toBe("Beginner");
    expect(profile.surfaceMode).toBe("Paved");
    expect(profile.cueMode).toBe("Overlay");
    expect(profile.overlayColor).toBe("#2F7CF6");
    expect(profile.colorPalette).toBe("Standard");
    expect(profile.brightness).toBe(1);
    expect(profile.contrast).toBe(1);
    expect(profile.audioOn).toBe(true);
    expect(profile.audioVolume).toBe(1);
    expect(profile.version).toBe(1);
  });
});

describe("toProfileInput", () => {
  // The server owns profileID, version and updatedAt, so the form never sends them.
  it("removes the fields the server controls", () => {
    const input = toProfileInput({ ...emptyProfile(), profileID: 7, version: 3, name: "P03" });

    expect(input).not.toHaveProperty("profileID");
    expect(input).not.toHaveProperty("version");
    expect(input).not.toHaveProperty("updatedAt");
    expect(input.name).toBe("P03");
  });
});

describe("apiRequest", () => {
  // Success case: a 200 answer comes back as parsed JSON, and the request
  // carries the sign-in cookie and a JSON content type.
  it("returns the parsed body on success", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ status: "ok" }), { status: 200 }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const result = await apiRequest<{ status: string }>("/api/my-profile", {
      method: "PUT",
      body: JSON.stringify({ name: "P03" }),
    });

    expect(result).toEqual({ status: "ok" });
    const [path, options] = fetchMock.mock.calls[0];
    expect(path).toBe("/api/my-profile");
    expect(options.credentials).toBe("include");
    expect(new Headers(options.headers).get("Content-Type")).toBe("application/json");
  });

  // Negative case: a 400 with validation errors throws an ApiError that
  // shows the first validation message to the user.
  it("throws an ApiError with the first validation message on 400", async () => {
    const problem = {
      title: "One or more validation errors occurred.",
      errors: { brightness: ["brightness must be between 0 and 1."] },
    };
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(JSON.stringify(problem), { status: 400 })),
    );

    const error = await apiRequest("/api/my-profile", { method: "PUT", body: "{}" }).catch(
      (e: unknown) => e,
    );

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).message).toBe("brightness must be between 0 and 1.");
  });

  // Negative case: when the backend is down, the user gets a clear message.
  it("throws an ApiError with status 0 when the server can't be reached", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    const error = await apiRequest("/health").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(0);
  });
});