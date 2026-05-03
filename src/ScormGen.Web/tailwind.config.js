/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.{razor,html,cshtml}'
  ],
  theme: {
    extend: {
      colors: {
        primary: '#98c93d',
        accent:  '#49c6e5',
        success: '#6ca437',
        danger:  '#da7552'
      }
    }
  },
  plugins: []
}
