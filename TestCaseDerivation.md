# Test Case Derivation — Create Booking

## Method Under Test: CreateBooking

Cucumber tests for this method can be run with:

```sh
dotnet test HotelBooking.UnitTests/HotelBooking.UnitTests.csproj --filter "CucumberTests"
```

### Equivalence Class Partitioning

I divided the input space into four classes. All values within a class produce the same behavior, so we only need one test per class.

- **EC1** — Start date is today or in the past: should throw an exception
- **EC2** — Start date is in the future, but end date is before start date: should throw an exception
- **EC3** — Valid dates and a room is available: should return `true`
- **EC4** — Valid dates but all rooms are occupied: should return `false`

### Test Cases

One scenario per equivalence class, implemented in `CucumberTests/Features/CreateBooking.feature`:

- **Booking with start date today should fail** — EC1
- **Booking with end date before start date should fail** — EC2
- **Booking with valid dates and available room should succeed** — EC3
- **Booking with valid dates but all rooms occupied should fail** — EC4
