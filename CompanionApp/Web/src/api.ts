export type VisionLossType = "Central" | "Peripheral" | "Both";
export type Severity = "Mild" | "Moderate" | "Severe";
export type AffectedEye = "Left" | "Right" | "Both";
export type SkillLevel = "Beginner" | "Intermediate" | "Advanced";
export type SurfaceMode = "Paved" | "Dirt" | "Snow";
export type CueMode = "Overlay" | "Audio" | "Both";
export type ColorPalette =
  | "Standard"
  | "Protanopia"
  | "Deuteranopia"
  | "Tritanopia";

export interface AthleteProfile {
  profileID: number;
  name: string;
  visionLossType: VisionLossType;
  severity: Severity;
  affectedEye: AffectedEye;
  height: number;
  skillLevel: SkillLevel;
  surfaceMode: SurfaceMode;
  cueMode: CueMode;
  overlayColor: string;
  colorPalette: ColorPalette;
  brightness: number;
  contrast: number;
  audioOn: boolean;
  audioVolume: number;
  notes: string;
  version: number;
  updatedAt: string;
}

export type AthleteProfileInput = Omit<
  AthleteProfile,
  "profileID" | "version" | "updatedAt"
>;

export function toProfileInput(profile: AthleteProfile): AthleteProfileInput {
  const {
    profileID: _profileID,
    version: _version,
    updatedAt: _updatedAt,
    ...editableFields
  } = profile;
  return editableFields;
}

export interface PairingCodeResponse {
  code: string;
  expiresAt: string;
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly payload: unknown,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

function responseMessage(payload: unknown, status: number): string {
  if (payload && typeof payload === "object") {
    const problem = payload as {
      detail?: string;
      title?: string;
      error?: string;
      errors?: Record<string, string[]>;
    };

    const firstValidationError = Object.values(problem.errors ?? {})
      .flat()
      .find(Boolean);

    return (
      firstValidationError ??
      problem.detail ??
      problem.error ??
      problem.title ??
      `The request failed (${status}).`
    );
  }

  if (typeof payload === "string" && payload.trim() && !payload.trimStart().startsWith("<")) {
    return payload.slice(0, 300);
  }

  if (status === 400) {
    return "The server could not read the request. Check the form and try again.";
  }
  if (status === 401) {
    return "Email or password did not match. Please try again.";
  }
  if (status === 409) {
    return "This profile changed on the headset. Reload it before saving again.";
  }
  if (status >= 500) {
    return "The Halo Vision service had a problem. Please try again.";
  }

  return `The request failed (${status}).`;
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;
  try {
    response = await fetch(path, {
      ...options,
      headers,
      credentials: "include",
    });
  } catch {
    throw new ApiError(
      "The Halo Vision service could not be reached. Start the backend and try again.",
      0,
      null,
    );
  }

  const text = await response.text();
  let payload: unknown = null;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = text;
    }
  }

  if (!response.ok) {
    throw new ApiError(responseMessage(payload, response.status), response.status, payload);
  }

  return payload as T;
}

export function emptyProfile(): AthleteProfile {
  return {
    profileID: 0,
    name: "",
    visionLossType: "Peripheral",
    severity: "Mild",
    affectedEye: "Both",
    height: 1.6,
    skillLevel: "Beginner",
    surfaceMode: "Paved",
    cueMode: "Overlay",
    overlayColor: "#2F7CF6",
    colorPalette: "Standard",
    brightness: 1,
    contrast: 1,
    audioOn: true,
    audioVolume: 1,
    notes: "",
    version: 1,
    updatedAt: "",
  };
}
