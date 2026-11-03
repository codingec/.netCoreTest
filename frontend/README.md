# booking-app - Frontend

Backend service for the booking-app project.
This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.17.

## Prerequisites

- Node.js
- npm / yarn / pnpm
- Git

Check the backend `package.json` for exact engine versions.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Building

To build the project run:

```bash
ng build
```

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Styling

The look of the app comes from one Angular Material 3 theme and a few shared SCSS partials in
`src/styles/`. No CSS framework or extra UI library is used.

| File | What it holds |
|---|---|
| `src/styles/_theme.scss` | The brand theme (blue primary, cyan accent). It follows the OS light/dark setting automatically. |
| `src/styles/_tokens.scss` | Spacing (`--space-1` … `--space-6`), `--content-max-width`, breakpoints and the `tablet-up`, `desktop-up` and `reduced-motion` mixins. |
| `src/styles/_layout.scss` | Global classes: `.page`, `.page-header`, `.card-grid`, `.clickable-card`, `.card-media`, `.card-text`, `.table-scroll`, `.section`. |

Rules for component styles:

- Use only theme tokens for color, shape and elevation (`var(--mat-sys-…)`) and spacing tokens
  (`var(--space-…)`). Never hard-code colors (hex, `rgb()`, `black`, …), or dark mode will break.
- Component `.scss` files can import the mixins with `@use 'tokens' as t;`
  (`src/styles` is on the Sass include path).
- Clickable cards use the `.clickable-card` class plus `tabindex="0"`, `role="button"` and
  Enter/Space handlers, so they work with the keyboard.

To check for hard-coded colors:

```bash
grep -rnE "#[0-9a-fA-F]{3,8}\b|rgba?\(|:\s*(black|white|gray|grey)\b" src/app --include=*.scss
```
