/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Match Blazor design system
        primary: '#3498db',
        secondary: '#6c757d',
        success: '#27ae60',
        danger: '#e74c3c',
        warning: '#f39c12',
        light: '#f8f9fa',
        dark: '#343a40',
        border: '#dee2e6',
        'text-muted': '#6c757d',
      },
    },
  },
  plugins: [],
}
