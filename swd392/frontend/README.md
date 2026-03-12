# Meal Preparation Service - Frontend

A mobile-responsive React application for the Meal Preparation Service, built with TypeScript, React Router, and Axios.

## Features

- **Responsive Design**: Mobile-first approach supporting screen widths from 320px to 2560px
- **White & Green Theme**: Consistent color scheme using CSS variables
- **Touch-Optimized**: Minimum 44x44px touch targets for mobile devices
- **Orientation Handling**: Preserves state during device rotation
- **Accessible**: WCAG-compliant with proper focus management and ARIA labels

## Tech Stack

- **React 19.2** - UI library
- **TypeScript** - Type safety
- **React Router** - Client-side routing
- **Axios** - HTTP client for API communication
- **Vite** - Build tool and dev server
- **CSS Variables** - Theming system

## Getting Started

### Prerequisites

- Node.js 18+ and npm

### Installation

```bash
npm install
```

### Environment Configuration

Create a `.env` file in the frontend directory:

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

### Development

Start the development server:

```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Build

Build for production:

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Project Structure

```
frontend/
├── src/
│   ├── components/
│   │   └── layout/
│   │       ├── Header.tsx
│   │       ├── Footer.tsx
│   │       ├── Container.tsx
│   │       └── Layout.tsx
│   ├── pages/
│   │   ├── Home.tsx
│   │   ├── MealPlans.tsx
│   │   ├── Recipes.tsx
│   │   ├── VirtualFridge.tsx
│   │   ├── Orders.tsx
│   │   └── Profile.tsx
│   ├── config/
│   │   └── api.ts
│   ├── styles/
│   │   └── theme.css
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── public/
├── .env
├── .env.example
├── package.json
├── tsconfig.json
└── vite.config.ts
```

## Responsive Breakpoints

- **Mobile**: 320px - 767px
- **Tablet**: 768px - 1023px
- **Desktop**: 1024px - 1439px
- **Wide**: 1440px - 2559px
- **Ultra-wide**: 2560px+

## Theme Variables

The application uses CSS variables for consistent theming. Key variables include:

- `--color-primary`: #2d7a3e (Green)
- `--color-secondary`: #ffffff (White)
- `--touch-target-min`: 44px
- `--font-size-mobile-min`: 14px

See `src/styles/theme.css` for the complete list.

## API Integration

The application communicates with the ASP.NET Core backend API. The Axios client is configured with:

- Base URL from environment variables
- Automatic JWT token injection
- Response interceptors for error handling
- 401 redirect to login page

## Mobile Optimization

- Hamburger menu below 768px
- Touch-friendly interactive elements (44x44px minimum)
- Minimum 14px font size on mobile
- Device orientation handling with state preservation
- Progressive image loading

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## License

Copyright © 2024 Meal Prep Service. All rights reserved.
