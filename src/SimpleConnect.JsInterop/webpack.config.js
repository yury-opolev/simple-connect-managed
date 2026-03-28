const path = require('path');

module.exports = {
    entry: './src/acs-interop.js',
    output: {
        filename: 'acs-interop.js',
        path: path.resolve(__dirname, '..', 'SimpleConnect.Client', 'wwwroot', 'js'),
        library: {
            name: 'AcsInterop',
            type: 'window'
        },
        clean: false
    },
    resolve: {
        extensions: ['.js']
    },
    performance: {
        hints: false,
        maxEntrypointSize: 512000,
        maxAssetSize: 512000
    }
};
