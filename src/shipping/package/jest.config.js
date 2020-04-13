module.exports = {
  testEnvironment: 'node',
  bail: true,
  verbose: true,
  setupFilesAfterEnv: [
    'jest-extended'
  ],
  testPathIgnorePatterns: [
    '/node_modules/'
  ],
  "moduleFileExtensions": ["js", "jsx", "json", "ts", "tsx"],
  "collectCoverage": true,
  "collectCoverageFrom": [
    "**/*.{ts,tsx,js,jsx}",
    "!**/tests/models/*.{ts,tsx,js,jsx}",
    "!**/node_modules/**",
    "!**/build/**",
    "!**/coverage/**"
  ],
  "transform": {
    "\\.ts$": "ts-jest"
  },
  "coverageThreshold": {
    "global": {
      "lines": 60,
    }
  },
  "coverageReporters": [
    "text",
    "text-summary"
  ]
};
