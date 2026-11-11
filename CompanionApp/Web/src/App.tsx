import { FormEvent, useEffect, useState } from "react";
import {
  ApiError,
  AthleteProfile,
  PairingCodeResponse,
  apiRequest,
  emptyProfile,
  toProfileInput,
} from "./api";

type Screen = "loading" | "auth" | "workspace";
type Page = "athletes" | "profile" | "headset" | "account";
type AuthMode = "login" | "register";
type Choice = { value: string; label: string; disabled?: boolean };

const choices = {
  visionLoss: [
    { value: "Central", label: "Central" },
    { value: "Peripheral", label: "Peripheral" },
    { value: "Both", label: "Both" },
  ],
  severity: [
    { value: "Mild", label: "Mild" },
    { value: "Moderate", label: "Moderate" },
    { value: "Severe", label: "Severe" },
  ],
  affectedEye: [
    { value: "Left", label: "Left" },
    { value: "Right", label: "Right" },
    { value: "Both", label: "Both" },
  ],
  skillLevel: [
    { value: "Beginner", label: "Beginner" },
    { value: "Intermediate", label: "Intermed." },
    { value: "Advanced", label: "Advanced" },
  ],
  surfaceMode: [
    { value: "Paved", label: "Paved" },
    { value: "Dirt", label: "Dirt" },
    { value: "Snow", label: "Snow", disabled: true },
  ],
  cueMode: [
    { value: "Overlay", label: "Overlay" },
    { value: "Audio", label: "Audio" },
    { value: "Both", label: "Both" },
  ],
  colorPalette: [
    { value: "Standard", label: "Standard" },
    { value: "Protanopia", label: "Protanopia" },
    { value: "Deuteranopia", label: "Deuteranopia" },
    { value: "Tritanopia", label: "Tritanopia" },
  ],
} satisfies Record<string, readonly Choice[]>;

function errorMessage(error: unknown): string {
  if (error instanceof Error) return error.message;
  return "Something went wrong. Please try again.";
}

function formatTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? ""
    : date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
}

function BrandMark() {
  return (
    <span className="brand-mark" aria-hidden="true">
      <svg viewBox="0 0 40 40" fill="none">
        <circle cx="20" cy="20" r="14" stroke="currentColor" strokeWidth="4" />
        <circle cx="20" cy="20" r="6" fill="currentColor" />
      </svg>
    </span>
  );
}

function ChoiceField({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly Choice[];
  onChange: (value: string) => void;
}) {
  const id = "choice-" + label.toLowerCase().replaceAll(" ", "-");
  return (
    <fieldset className="field choice-field">
      <legend id={id}>{label}</legend>
      <div className="segmented-control" role="group" aria-labelledby={id}>
        {options.map((option) => (
          <button
            key={option.value}
            className={value === option.value ? "is-selected" : ""}
            type="button"
            aria-pressed={value === option.value}
            disabled={option.disabled}
            onClick={() => onChange(option.value)}
          >
            {option.label}
          </button>
        ))}
      </div>
    </fieldset>
  );
}

function RangeField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  const id = "range-" + label.toLowerCase().replaceAll(" ", "-");
  return (
    <label className="field range-field" htmlFor={id}>
      <span className="field-label">
        {label}
        <strong>{Math.round(value * 100)}%</strong>
      </span>
      <input
        id={id}
        type="range"
        min="0"
        max="1"
        step="0.05"
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
      />
    </label>
  );
}

function Summary({ profile }: { profile: AthleteProfile }) {
  return (
    <span className="athlete-summary">
      {profile.visionLossType} loss · {profile.severity.toLowerCase()} ·{" "}
      {profile.surfaceMode.toLowerCase()} · {profile.cueMode.toLowerCase()}
    </span>
  );
}

export default function App() {
  const [screen, setScreen] = useState<Screen>("loading");
  const [page, setPage] = useState<Page>("athletes");
  const [authMode, setAuthMode] = useState<AuthMode>("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authError, setAuthError] = useState("");
  const [authBusy, setAuthBusy] = useState(false);
  const [profile, setProfile] = useState<AthleteProfile>(emptyProfile);
  const [hasProfile, setHasProfile] = useState(false);
  const [saveBusy, setSaveBusy] = useState(false);
  const [pairBusy, setPairBusy] = useState(false);
  const [pairingCode, setPairingCode] = useState<PairingCodeResponse | null>(null);
  const [now, setNow] = useState(Date.now());
  const [feedback, setFeedback] = useState("");
  const [feedbackError, setFeedbackError] = useState(false);

  async function loadWorkspace() {
    setFeedback("");
    try {
      const current = await apiRequest<AthleteProfile>("/api/my-profile/");
      setProfile({ ...emptyProfile(), ...current });
      setHasProfile(true);
      setPage("athletes");
      setScreen("workspace");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setScreen("auth");
        return;
      }
      if (error instanceof ApiError && error.status === 404) {
        setProfile(emptyProfile());
        setHasProfile(false);
        setPage("athletes");
        setScreen("workspace");
        return;
      }
      setAuthError(errorMessage(error));
      setScreen("auth");
    }
  }

  useEffect(() => {
    void loadWorkspace();
  }, []);

  useEffect(() => {
    if (!pairingCode) return;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [pairingCode]);

  const secondsLeft = pairingCode
    ? Math.max(0, Math.ceil((Date.parse(pairingCode.expiresAt) - now) / 1000))
    : 0;
  const activeCode = Boolean(pairingCode && secondsLeft > 0);
  const setField = <K extends keyof AthleteProfile>(
    key: K,
    value: AthleteProfile[K],
  ) => setProfile((current) => ({ ...current, [key]: value }));

  async function handleAuth(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setAuthBusy(true);
    setAuthError("");
    try {
      if (authMode === "register") {
        await apiRequest("/api/auth/register", {
          method: "POST",
          body: JSON.stringify({ email, password }),
        });
      }
      await apiRequest("/api/auth/login?useCookies=true", {
        method: "POST",
        body: JSON.stringify({ email, password }),
      });
      await loadWorkspace();
    } catch (error) {
      setAuthError(errorMessage(error));
    } finally {
      setAuthBusy(false);
    }
  }

  async function handleLogout() {
    try {
      await apiRequest("/api/auth/logout", { method: "POST" });
    } catch {
      // The browser can still return to sign-in if the session has expired.
    }
    setPairingCode(null);
    setProfile(emptyProfile());
    setHasProfile(false);
    setPassword("");
    setFeedback("");
    setPage("athletes");
    setScreen("auth");
  }

  async function saveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaveBusy(true);
    setFeedback("");
    try {
      const saved = await apiRequest<AthleteProfile>("/api/my-profile/", {
        method: "PUT",
        body: JSON.stringify(toProfileInput(profile)),
      });
      setProfile({ ...emptyProfile(), ...saved });
      setHasProfile(true);
      setFeedback("Athlete saved.");
      setFeedbackError(false);
      setPage("athletes");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409 && error.payload) {
        setProfile({ ...emptyProfile(), ...(error.payload as AthleteProfile) });
        setFeedback("A newer profile was saved on the headset. Review it and save again.");
      } else {
        setFeedback(errorMessage(error));
      }
      setFeedbackError(true);
    } finally {
      setSaveBusy(false);
    }
  }

  async function setGuidance(active: boolean) {
    setFeedback("");

    try {
      await apiRequest("/api/guidance", {
        method: "PUT",
        body: JSON.stringify({ active }),
      });

      setFeedback(
        active
          ? "Start guidance command sent."
          : "Stop guidance command sent."
      );

      setFeedbackError(false);
    } catch (error) {
      setFeedback(errorMessage(error));
      setFeedbackError(true);
    }
  }

  async function createPairingCode() {
    setPairBusy(true);
    setFeedback("");
    try {
      const response = await apiRequest<PairingCodeResponse>("/api/pairing/codes", {
        method: "POST",
      });
      setPairingCode(response);
      setNow(Date.now());
      setFeedback("Code ready. Enter it on the headset to finish pairing.");
      setFeedbackError(false);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) setScreen("auth");
      setFeedback(errorMessage(error));
      setFeedbackError(true);
    } finally {
      setPairBusy(false);
    }
  }

  async function copyPairingCode() {
    if (!pairingCode) return;
    try {
      await navigator.clipboard.writeText(pairingCode.code);
      setFeedback("Pairing code copied.");
      setFeedbackError(false);
    } catch {
      setFeedback("Select the code and copy it manually.");
      setFeedbackError(true);
    }
  }

  if (screen === "loading") {
    return (
      <main className="loading-screen" aria-live="polite">
        <BrandMark />
        <span>Opening Halo Vision…</span>
      </main>
    );
  }

  if (screen === "auth") {
    return (
      <main className="app-canvas auth-canvas">
        <section className="app-frame auth-frame" aria-labelledby="auth-title">
          <div className="auth-brand">
            <BrandMark />
            <h1 id="auth-title">Halo Vision</h1>
            <p>Guide app</p>
          </div>

          <form className="auth-form" onSubmit={handleAuth}>
            <label className="field" htmlFor="account-email">
              <span className="field-label">Email</span>
              <input
                id="account-email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="you@example.com"
                required
              />
            </label>
            <label className="field" htmlFor="account-password">
              <span className="field-label">Password</span>
              <input
                id="account-password"
                type="password"
                autoComplete={authMode === "login" ? "current-password" : "new-password"}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
              {authMode === "register" && (
                <small className="field-hint">
                  Use at least 6 characters, including upper and lowercase letters, a number, and a symbol.
                </small>
              )}
              {authError && (
                <small className="inline-error" role="alert">{authError}</small>
              )}
            </label>
            <button className="button button-primary button-full" disabled={authBusy}>
              {authBusy ? "Please wait…" : authMode === "login" ? "Sign in" : "Create account"}
            </button>
          </form>

          <button
            className="text-button auth-switch"
            type="button"
            onClick={() => {
              setAuthMode(authMode === "login" ? "register" : "login");
              setAuthError("");
            }}
          >
            {authMode === "login" ? "Create a guide account" : "Back to sign in"}
          </button>
        </section>
      </main>
    );
  }

  const activeTab =
    page === "profile" || page === "athletes"
      ? "athletes"
      : page === "headset"
        ? "headset"
        : "account";

  return (
    <main className="app-canvas">
      <section className="app-frame" aria-label="Halo Vision guide app">
        <header className="app-topbar">
          <div className="brand-lockup">
            <BrandMark />
            <span>Halo Vision</span>
          </div>
          <span className="account-label">{email || "Guide account"}</span>
        </header>

        <div className="app-content">
          {page === "athletes" && (
            <>
              <div className="screen-heading">
                <div>
                  <p className="eyebrow">GUIDE APP</p>
                  <h1>Athletes</h1>
                </div>
                <button
                  className="text-button new-button"
                  type="button"
                  onClick={() => {
                    if (hasProfile) {
                      setFeedback("This account currently supports one athlete profile.");
                      setFeedbackError(false);
                      return;
                    }
                    setProfile(emptyProfile());
                    setFeedback("");
                    setPage("profile");
                  }}
                  aria-label="Add athlete"
                >
                  +
                </button>
              </div>

              {feedback && (
                <div className={"feedback " + (feedbackError ? "feedback-error" : "feedback-success")}
                  role={feedbackError ? "alert" : "status"}>
                  {feedback}
                </div>
              )}

              {hasProfile ? (
                <button
                  className="athlete-card"
                  type="button"
                  onClick={() => {
                    setFeedback("");
                    setPage("profile");
                  }}
                >
                  <span className="athlete-card-copy">
                    <strong>{profile.name}</strong>
                    <Summary profile={profile} />
                    <small>
                      {profile.updatedAt ? "Saved " + formatTime(profile.updatedAt) : "Saved to your account"}
                    </small>
                  </span>
                  <span className="card-chevron" aria-hidden="true">›</span>
                </button>
              ) : (
                <section className="empty-state">
                  <div className="empty-mark" aria-hidden="true">+</div>
                  <h2>No athlete yet</h2>
                  <p>Create one athlete profile for this account.</p>
                  <button
                    className="button button-primary button-full"
                    type="button"
                    onClick={() => {
                      setProfile(emptyProfile());
                      setPage("profile");
                    }}
                  >
                    Create athlete
                  </button>
                </section>
              )}
            </>
          )}

          {page === "profile" && (
            <>
              <div className="screen-toolbar">
                <button className="back-button" type="button" onClick={() => setPage("athletes")}>
                  ‹ Athletes
                </button>
                <button className="text-button save-link" type="submit" form="profile-form" disabled={saveBusy}>
                  {saveBusy ? "Saving…" : "Save"}
                </button>
              </div>
              <div className="screen-heading profile-title">
                <div>
                  <p className="eyebrow">ATHLETE PROFILE</p>
                  <h1>{hasProfile ? profile.name || "Edit athlete" : "New athlete"}</h1>
                </div>
              </div>
              {feedback && (
                <div className={"feedback " + (feedbackError ? "feedback-error" : "feedback-success")}
                  role={feedbackError ? "alert" : "status"}>
                  {feedback}
                </div>
              )}

              <form id="profile-form" className="profile-form" onSubmit={saveProfile}>
                <label className="field" htmlFor="profile-name">
                  <span className="field-label">Name</span>
                  <input
                    id="profile-name"
                    value={profile.name}
                    onChange={(event) => setField("name", event.target.value)}
                    placeholder="Athlete name"
                    autoComplete="name"
                    required
                  />
                </label>

                <ChoiceField
                  label="Vision loss"
                  value={profile.visionLossType}
                  options={choices.visionLoss}
                  onChange={(value) => setField("visionLossType", value as AthleteProfile["visionLossType"])}
                />
                <ChoiceField
                  label="Severity"
                  value={profile.severity}
                  options={choices.severity}
                  onChange={(value) => setField("severity", value as AthleteProfile["severity"])}
                />
                <ChoiceField
                  label="Affected eye"
                  value={profile.affectedEye}
                  options={choices.affectedEye}
                  onChange={(value) => setField("affectedEye", value as AthleteProfile["affectedEye"])}
                />

                <div className="form-row">
                  <label className="field" htmlFor="profile-height">
                    <span className="field-label">Eye height <small>(m)</small></span>
                    <input
                      id="profile-height"
                      type="number"
                      min="0.5"
                      max="2.5"
                      step="0.01"
                      value={profile.height}
                      onChange={(event) => setField("height", Number(event.target.value))}
                      required
                    />
                  </label>
                  <ChoiceField
                    label="Skill"
                    value={profile.skillLevel}
                    options={choices.skillLevel}
                    onChange={(value) => setField("skillLevel", value as AthleteProfile["skillLevel"])}
                  />
                </div>

                <ChoiceField
                  label="Surface"
                  value={profile.surfaceMode}
                  options={choices.surfaceMode}
                  onChange={(value) => setField("surfaceMode", value as AthleteProfile["surfaceMode"])}
                />
                <ChoiceField
                  label="Cue mode"
                  value={profile.cueMode}
                  options={choices.cueMode}
                  onChange={(value) => setField("cueMode", value as AthleteProfile["cueMode"])}
                />

                <div className="form-row color-row">
                  <label className="field" htmlFor="overlay-color">
                    <span className="field-label">Overlay color</span>
                    <span className="color-control">
                      <input
                        id="overlay-color"
                        type="color"
                        value={profile.overlayColor}
                        onChange={(event) => setField("overlayColor", event.target.value.toUpperCase())}
                      />
                      <code>{profile.overlayColor}</code>
                    </span>
                  </label>
                  <ChoiceField
                    label="Palette"
                    value={profile.colorPalette}
                    options={choices.colorPalette}
                    onChange={(value) => setField("colorPalette", value as AthleteProfile["colorPalette"])}
                  />
                </div>

                <div className="form-row range-row">
                  <RangeField
                    label="Brightness"
                    value={profile.brightness}
                    onChange={(value) => setField("brightness", value)}
                  />
                  <RangeField
                    label="Contrast"
                    value={profile.contrast}
                    onChange={(value) => setField("contrast", value)}
                  />
                </div>

                <label className="toggle-row" htmlFor="audio-on">
                  <span>
                    <strong>Audio on</strong>
                    <small>Use audio cues when available.</small>
                  </span>
                  <input
                    id="audio-on"
                    type="checkbox"
                    checked={profile.audioOn}
                    onChange={(event) => setField("audioOn", event.target.checked)}
                  />
                </label>
                {profile.audioOn && (
                  <RangeField
                    label="Volume"
                    value={profile.audioVolume}
                    onChange={(value) => setField("audioVolume", value)}
                  />
                )}

                <label className="field" htmlFor="profile-notes">
                  <span className="field-label">Notes</span>
                  <textarea
                    id="profile-notes"
                    rows={3}
                    value={profile.notes}
                    onChange={(event) => setField("notes", event.target.value)}
                    placeholder="Useful notes for the guide"
                  />
                </label>
                <p className="version-note">
                  {hasProfile ? "Profile version " + profile.version : "Saved to this guide account."}
                </p>
              </form>
            </>
          )}

          {page === "headset" && (
            <>
              <div className="screen-heading">
                <div>
                  <p className="eyebrow">HEADSET</p>
                  <h1>Pair a headset</h1>
                </div>
              </div>
              {feedback && (
                <div className={"feedback " + (feedbackError ? "feedback-error" : "feedback-success")}
                  role={feedbackError ? "alert" : "status"}>
                  {feedback}
                </div>
              )}

              <section className="pair-card">
                {activeCode ? (
                  <>
                    <p className="eyebrow">ONE-TIME CODE</p>
                    <div className="pair-code" aria-label={"Pairing code " + pairingCode?.code}>
                      {pairingCode?.code}
                    </div>
                    <p className="code-expiry">
                      Expires in {Math.floor(secondsLeft / 60)}:
                      {String(secondsLeft % 60).padStart(2, "0")}
                      {pairingCode && formatTime(pairingCode.expiresAt)
                        ? " · " + formatTime(pairingCode.expiresAt)
                        : ""}
                    </p>
                    <button className="button button-secondary button-full" type="button" onClick={copyPairingCode}>
                      Copy code
                    </button>
                  </>
                ) : (
                  <>
                    <div className="headset-symbol" aria-hidden="true">⌁</div>
                    <h2>Connect your HoloLens 2</h2>
                    <p>A code is valid for 10 minutes and can be used once.</p>
                  </>
                )}
              </section>
              <button
                className="button button-primary button-full pair-button"
                type="button"
                disabled={!hasProfile || pairBusy || activeCode}
                onClick={createPairingCode}
              >
                {pairBusy ? "Creating code…" : activeCode ? "Code ready" : "Generate pairing code"}
              </button>
              <section className="pair-card">
                <h2>Guidance Controls</h2>
                <p>Control the guidance display on the paired headset.</p>

                <button
                  className="button button-primary button-full"
                  type="button"
                  disabled={!hasProfile}
                  onClick={() => setGuidance(true)}
                >
                  Start Guidance
                </button>

                <button
                  className="button button-secondary button-full"
                  type="button"
                  disabled={!hasProfile}
                  onClick={() => setGuidance(false)}
                >
                  Stop Guidance
                </button>
              </section>
            </>
          )}

          {page === "account" && (
            <>
              <div className="screen-heading">
                <div>
                  <p className="eyebrow">ACCOUNT</p>
                  <h1>Guide account</h1>
                </div>
              </div>
              <section className="account-card">
                <span className="account-avatar" aria-hidden="true">
                  {(email || "G").slice(0, 1).toUpperCase()}
                </span>
                <div>
                  <strong>{email || "Signed in"}</strong>
                  <small>Halo Vision guide account</small>
                </div>
              </section>
              <button className="button button-secondary button-full signout-button" type="button" onClick={handleLogout}>
                Sign out
              </button>
            </>
          )}
        </div>

        <nav className="bottom-nav" aria-label="Main navigation">
          {[
            { page: "athletes" as Page, label: "Athletes", symbol: "A" },
            { page: "headset" as Page, label: "Headset", symbol: "H" },
            { page: "account" as Page, label: "Account", symbol: "•" },
          ].map((tab) => (
            <button
              key={tab.page}
              className={activeTab === tab.page ? "nav-tab is-current" : "nav-tab"}
              type="button"
              aria-current={activeTab === tab.page ? "page" : undefined}
              onClick={() => {
                setFeedback("");
                setPage(tab.page);
              }}
            >
              <span className="nav-symbol" aria-hidden="true">{tab.symbol}</span>
              <span>{tab.label}</span>
            </button>
          ))}
        </nav>
      </section>
    </main>
  );
}
