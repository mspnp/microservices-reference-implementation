var gulp = require('gulp');
var ts = require('gulp-typescript');
var nodemon = require('gulp-nodemon');
var del = require('del');
var mocha = require('gulp-mocha');

gulp.task('set-env', gulp.series(function(done) {
    process.env.CONNECTION_STRING = "mongodb://packagedb:27017/local";
    process.env.COLLECTION_NAME = "packages";
    process.env.CORRELATION_HEADER = "x-b3-traceid";
    process.env.LOG_LEVEL = "debug"
    done();
}));

// Generate types that can be used when working with api definitions
gulp.task ('create-api-classes', gulp.series(function() {

    let generated = "// GENERATED FILE - DO NOT EDIT\n\n";

    api = require('./app/api.json');
    for (var gtype in api.definitions)
    {
        generated += "export class " + gtype + "\n{\n";
        let definition = api.definitions[gtype];
        for (var gprop in definition.properties)
        {
            let propType = definition.properties[gprop]['type'];

            // If this is a reference to a class we will use that
            if (propType == null) {
                propType = definition.properties[gprop]['$ref'].split('/').splice(-1);
            }

            // convert swagger integer to
            if (propType == "integer")
                propType = "number";

            generated += "  " + gprop + ":" + propType + "\n";
            console.log(gprop);
        }
        generated += "}\n\n";
    }

    var fs = require('fs');
    fs.writeFile("./app/models/api-models.ts", generated, function(err) {
        if(err) {
            return console.log(err);
        }
    });
}));

// delete all build output files
gulp.task('clean', gulp.series(function (done) {
    return del(['.bin/'], done);
 }));
 
gulp.task('build', gulp.series('clean', function () {
    gulp.src('**/*.json', {'cwd':'app'})
        .pipe(gulp.dest('.bin/app'));

    var tsProject = ts.createProject('tsconfig.json');

    var tsResult = gulp.src("**/*.ts", {'cwd':'app'})
        .pipe(tsProject());

    return tsResult.js.pipe(gulp.dest('.bin/app'));
}));

gulp.task('serve', gulp.series('build', 'set-env', function (cb) {
    return nodemon({
        script: '.bin/app/main.js',
        ext: 'ts json',
        watch: 'app',
        env: { "PORT": 80 },
        tasks: ["build"],
        env: { 'NODE_ENV': 'development' }
    })
}));

gulp.task('build-int-test', gulp.series('build', function  () {
    var tsProject = ts.createProject('tsconfig.json');

    var tsResult = gulp.src("test/**/*.ts", {'base':'.'})
        .pipe(tsProject());

    return tsResult.js.pipe(gulp.dest('.bin'));
}));

gulp.task('int-test', gulp.series('build-int-test', 'set-env', function() {
    return gulp.src("test/**/*.js", { cwd: '.bin' })
       .pipe(mocha({ reporter: 'list', exit: true }));
}));
