# Toggl Mobile

This repository contains the source code for building the native Toggl Android and iOS applications.
These applications are built using [Xamarin](http://xamarin.com/) products to allow for maximum
code reuse and faster development times.

## Repository structure

The repository consists of the following projects:

- Phoebe - shared code project that contains data models and business logic across platforms.
- Joey - Android application (UI & Android specific code).
- Ross - iOS application (UI & iOS specific code).
- Chandler - Simple Android wear app.
- Emma - iOS widget app.
- Tests - unit tests for testing code in Phoebe

## Setting up

Installing [brew](http://brew.sh/), [bitrise CI](https://www.bitrise.io/cli) and setting some initial configuration you can generate the binaries for each platform.
	
	$ brew install bitrise
	$ bitrise run test 
	$ bitrise run android

## Contributing

Want to contribute? Great! Just [fork](https://github.com/toggl/mobile/fork) the project, make your
changes and submit a [pull request](https://github.com/toggl/mobile/pulls).

### Code style

There is a pre-commit git hook, that prevents commits with invalid code style. To reformat all of
the source files, just run:

	$ make format

### Beta testing

Do you want to put our app to the limits? There is a bug that and you know how to simulate it? Join to our Beta tester community and give us your feedback, just write us at [support@toggl.com](mailto:support@toggl.com)

Download the last [Android Beta](https://tsfr.io/xrkvaq)

Download the last [iOS Beta](https://tsfr.io/w7k2a8) (closed beta, contact us first!)

## We are hiring!

Check out our [jobs page](http://jobs.toggl.com/) for current open positions.

## License

The code in this repository is licensed under the [BSD 3-clause license](https://github.com/toggl/mobile/blob/master/LICENSE).
