// Karma configuration file, see link for more information
// https://karma-runner.github.io/1.0/config/configuration-file.html

const fs = require('fs');
const path = require('path');

// Chrome is not installed on the development machine (research R0). When CHROME_BIN is unset and
// no Chrome is found, fall back to Microsoft Edge, which the ChromeHeadless launcher can drive.
if (!process.env.CHROME_BIN) {
  const candidates = [
    `${process.env['ProgramFiles']}\\Google\\Chrome\\Application\\chrome.exe`,
    `${process.env['ProgramFiles(x86)']}\\Google\\Chrome\\Application\\chrome.exe`,
    `${process.env['ProgramFiles(x86)']}\\Microsoft\\Edge\\Application\\msedge.exe`,
    `${process.env['ProgramFiles']}\\Microsoft\\Edge\\Application\\msedge.exe`,
  ];
  const found = candidates.find((candidate) => fs.existsSync(candidate));
  if (found) {
    process.env.CHROME_BIN = found;
  }
}

module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    client: {
      jasmine: {},
    },
    jasmineHtmlReporter: {
      suppressAll: true // removes the duplicated traces
    },
    coverageReporter: {
      dir: path.join(__dirname, './coverage/prescription-refill-web'),
      subdir: '.',
      reporters: [
        { type: 'html' },
        { type: 'text-summary' }
      ]
    },
    reporters: ['progress', 'kjhtml'],
    browsers: ['ChromeHeadless'],
    restartOnFileChange: true
  });
};
