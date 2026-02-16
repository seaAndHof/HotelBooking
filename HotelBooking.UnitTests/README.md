## Test Design

### Choice of Mocking Framework
- We have chosen Moq due to previous experience with it. 


### Method Under Test: GetFullyOccupiedDates:
Tests for this method can be run with: 

```sh
dotnet test HotelBooking.UnitTests/HotelBooking.UnitTests.csproj --filter "GetFullyOccupiedDatesTests
```

#### **Observable Behavior**:

We aim to test only the observable behavior of this method. If something is irrelevant to the caller, it's irrelevant to us in the test.
As a caller of the method, we see two observable behaviors, either:
- The happy path: the method is called with valid parameters and returns the expected list of dates.
- The failure path: the method is called with invalid parameters and throws an exception.

So we will need two test methods. We're naming the tests using the pattern: Method_State_Expectation
- Happy Path: _GetFullyOccupiedDates_ValidDateRange_ReturnsExpectedDates_
- Failure path: _GetFullyOccupiedDates_InvalidDateRange_ThrowsArgumentException_

#### **Test Cases**:
The failure path is simple. Parameters are only invalid if the start date is after the end date.

For the happy path, we need to use data-driven testing to cover various scenarios.
The list of scenarios we want to cover are these (not exhaustive, but it's enough to give us confidence in the method):
- No rooms, no bookings: should return an empty list
- Some rooms, no bookings: should return an empty list
- Single day range (startDate == endDate), no bookings: empty list
- Single day range, fully occupied: return that date
- Single day range, partially occupied: empty list
- Multi-day range, no bookings: empty list
- Multi-day range, some days fully occupied, some not: return only the fully occupied dates
- Multi-day range, all days fully occupied: return all dates in the range
- Booking starts before range, ends during range: correct occupied dates within range
- Booking starts during range, ends after range → correct occupied dates within range
- Mix of active and inactive bookings: only active bookings should be counted

### Method Under Test: CreateBooking
Tests for this method can be run with:

```sh
dotnet test HotelBooking.UnitTests/HotelBooking.UnitTests.csproj --filter "CreateBooking"
```

#### **Observable Behavior**:

As a caller of the method, we see two observable behaviors:
- The happy path: a room is available, the booking is created, and the method returns `true`.
- The failure path (no room): all rooms are occupied for the requested dates, and the method returns `false`.
- The failure path (invalid dates): the method is called with invalid dates (start date in the past or after end date) and throws an `ArgumentException`.

#### **Test Cases**:
- Room available, no existing bookings: should return `true`
- All rooms occupied for the period: should return `false`
- Invalid dates (start date today or start date after end date): should throw `ArgumentException`

