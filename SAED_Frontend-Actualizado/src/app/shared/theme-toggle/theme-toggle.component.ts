import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ThemeService } from 'src/app/core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      type="button"
      class="theme-toggle"
      [class.compact]="compact"
      [attr.aria-label]="actionLabel"
      [attr.aria-pressed]="theme.darkMode()"
      [title]="actionLabel"
      (click)="theme.toggle()">
      <i class="bi" [class.bi-sun-fill]="theme.darkMode()" [class.bi-moon-stars-fill]="!theme.darkMode()"></i>
      <span *ngIf="!compact">{{ theme.darkMode() ? 'Tema claro' : 'Tema oscuro' }}</span>
    </button>
  `,
  styles: [`
    :host {
      display: block;
      min-width: 0;
    }

    .theme-toggle {
      width: 100%;
      min-height: 40px;
      display: inline-flex;
      align-items: center;
      justify-content: flex-start;
      gap: 0.65rem;
      padding: 0.55rem 0.75rem;
      border: 1px solid var(--saed-border);
      border-radius: 6px;
      background: var(--saed-surface);
      color: var(--saed-text);
      cursor: pointer;
      font-size: 0.82rem;
      font-weight: 700;
      line-height: 1;
      transition: background-color var(--saed-transition), border-color var(--saed-transition), color var(--saed-transition);
    }

    .theme-toggle:hover {
      background: var(--saed-bg-soft);
      border-color: var(--saed-primary);
      color: var(--saed-primary);
    }

    .theme-toggle:focus-visible {
      outline: 3px solid rgba(15, 148, 126, 0.28);
      outline-offset: 2px;
    }

    .theme-toggle i {
      width: 1rem;
      flex: 0 0 1rem;
      text-align: center;
      font-size: 1rem;
    }

    .theme-toggle.compact {
      width: 40px;
      height: 40px;
      min-height: 40px;
      justify-content: center;
      padding: 0;
      border-radius: 50%;
    }
  `]
})
export class ThemeToggleComponent {
  @Input() compact = false;

  constructor(readonly theme: ThemeService) {
    this.theme.initialize();
  }

  get actionLabel(): string {
    return this.theme.darkMode() ? 'Cambiar a tema claro' : 'Cambiar a tema oscuro';
  }
}
