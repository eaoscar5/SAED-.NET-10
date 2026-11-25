import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, signal } from '@angular/core';

type ThemePreference = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'saed-theme';
  private initialized = false;

  readonly darkMode = signal(false);

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  initialize(): void {
    if (this.initialized || !isPlatformBrowser(this.platformId)) return;

    const stored = this.readStoredPreference();
    const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
    this.applyTheme(stored ? stored === 'dark' : prefersDark);
    this.initialized = true;
  }

  toggle(): void {
    this.initialize();
    const nextDarkMode = !this.darkMode();
    this.applyTheme(nextDarkMode);

    try {
      localStorage.setItem(this.storageKey, nextDarkMode ? 'dark' : 'light');
    } catch {
      // El tema sigue funcionando aunque el navegador bloquee almacenamiento local.
    }
  }

  private applyTheme(darkMode: boolean): void {
    const root = this.document.documentElement;
    root.classList.toggle('saed-dark', darkMode);
    root.dataset['theme'] = darkMode ? 'dark' : 'light';
    root.style.colorScheme = darkMode ? 'dark' : 'light';
    this.darkMode.set(darkMode);
  }

  private readStoredPreference(): ThemePreference | null {
    try {
      const value = localStorage.getItem(this.storageKey);
      return value === 'dark' || value === 'light' ? value : null;
    } catch {
      return null;
    }
  }
}
