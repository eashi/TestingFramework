# 1.5.0 (14th April 2023)

LogicAppUnit Testing Framework:

- Invoked child workflows are now mocked using HTTP actions. This means that the dependencies between a parent workflow and child workflows can be broken to enable better unit testing of the parent workflow.
- `LogicAppUnit.TestRunner` does not assume that the name of the HTTP trigger is `manual`, it now retrieves the name from the workflow definition.
  - This change is needed because the new Standard Logic App designer allows a developer to edit the name of the HTTP trigger. In all previous versions of the designer the trigger name was set to `manual` and could not be changed.
- Non-HTTP triggers that are replaced with HTTP triggers now have the same name as the original trigger. Previously, the name of the HTTP trigger was set to `manual`.

LogicAppUnit.Samples.LogicApps.Tests:

- Added an `InvokeWorkflowTest` workflow and unit tests to demonstrate the use of the testing framework with child workflows that are invoked from a parent workflow.


# 1.4.0 (21st February 2023)

LogicAppUnit Testing Framework:

- Changed the logic that updates the `connectionRuntimeUrl` for Managed API connectors so that it works with URL values that include `@appsetting()` references. [[Issue #9](https://github.com/LogicAppUnit/TestingFramework/issues/9)]


# 1.3.0 (1st February 2023)

LogicAppUnit Testing Framework:

- Added methods to `LogicAppUnit.TestRunner` to allow tests to access the tracked properties that are created by an action. This includes action repetitions.
  - This is only available for stateful workflows because tracked properties are never recorded in the run history for stateless workflows.
- Updated `LogicAppUnit.Helper.ContentHelper.FormatJson(string)` so that any references to the local server name are replaced with `localhost`.

LogicAppUnit.Samples.LogicApps.Tests:

- Updated the `HttpWorkflowTest` workflow and unit tests to include tracked properties.


# 1.2.0 (9th January 2023)

LogicAppUnit Testing Framework:

- Added methods to `LogicAppUnit.TestRunner` to allow tests to assert actions that run in an `Until` loop or a `ForEach` loop. These actions are known as action repetitions.
- Added methods to `LogicAppUnit.TestRunner` to allow tests to access the input and output messages for an action. This includes action repetitions.
- Added an interface `LogicAppUnit.ITestRunner` and updated `LogicAppUnit.TestRunner` to implement this interface. This interface has been added to allow for the implementation of other test runners in the future.
- Method `LogicAppUnit.WorkflowTestBase.CreateTestRunner()` returns an instance of `LogicAppUnit.ITestRunner` and not `LogicAppUnit.TestRunner`.

LogicAppUnit.Samples.LogicApps.Tests:

- Added a `LoopWorkflowTest` workflow and unit tests to demonstrate the use of the testing framework with a workflow containing actions in an `Until` loop and a `ForEach` loop.


# 1.1.0 (16th December 2022)

LogicAppUnit Testing Framework:

- Changed the visibility of the `LogicAppUnit.Hosting` classes from `public` to `internal`. These classes are not for use by test authors.
- Added a new configuration option `azurite.enableAzuritePortCheck` to `testConfiguration.json` to enable or disable the Azurite port checks.
- Refactored the internal classes that update the workflow definition, local settings and connection files.
- The Test Runner (`LogicAppUnit.TestRunner`) now supports workflow HTTP triggers with relative paths.
- Improved handling of stateless workflows:
  - Added a new configuration option `workflow.autoConfigureWithStatelessRunHistory` to `testConfiguration.json` to control whether the testing framework automatically configures the workflow `OperationOptions` setting to `WithStatelessRunHistory`. If this option is not set for stateless workflows, the workflow run history is not stored. The default value for this configuration option is `true`.
  - If a stateless workflow is tested and the `OperationOptions` setting is not set to `WithStatelessRunHistory`, and `workflow.autoConfigureWithStatelessRunHistory` is set to `false`, the test fails with a `TestException` error.
- Added the `TestRunner.WorkflowClientTrackingId` property so that tests can assert a workflow's client tracking id.
- Improvements to `Readme.md`.

LogicAppUnit.Samples.LogicApps.Tests:

- Added a `StatelessWorkflowTest` workflow and unit tests to demonstrate the use of the testing framework with a stateless workflow, a custom client tracking id and a relative path configured in the HTTP trigger.


# 1.0.0 (9th December 2022)

- Initial version.
