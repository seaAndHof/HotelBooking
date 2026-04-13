using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Reqnroll;
using Xunit;

namespace HotelBooking.UnitTests.CucumberTests.StepDefinitions
{
    [Binding]
    public class CreateBookingStepDefinitions
    {
        private readonly Mock<IRepository<Booking>> bookingRepoMock = new();
        private readonly Mock<IRepository<Room>> roomRepoMock = new();
        private readonly IBookingManager bookingManager;

        private Booking booking;
        private bool? createResult;
        private Exception caughtException;

        public CreateBookingStepDefinitions()
        {
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room A" },
                new Room { Id = 2, Description = "Room B" },
                new Room { Id = 3, Description = "Room C" },
            };
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

            bookingManager = new BookingManager(bookingRepoMock.Object, roomRepoMock.Object);
            booking = new Booking { CustomerId = 1 };
        }

        // --- Given steps ---

        [Given("the hotel has rooms available")]
        public void GivenTheHotelHasRoomsAvailable()
        {
            // Rooms are set up in the constructor
        }

        [Given("the start date is today and end date is {int} days from now")]
        public void GivenStartDateIsTodayAndEndDate(int endDaysFromNow)
        {
            booking.StartDate = DateTime.Today;
            booking.EndDate = DateTime.Today.AddDays(endDaysFromNow);
        }

        [Given("the start date is {int} days from now and end date is {int} days from now")]
        public void GivenStartDateAndEndDateDaysFromNow(int startDays, int endDays)
        {
            booking.StartDate = DateTime.Today.AddDays(startDays);
            booking.EndDate = DateTime.Today.AddDays(endDays);
        }

        [Given("the start date is tomorrow and end date is {int} days from now")]
        public void GivenStartDateIsTomorrowAndEndDate(int endDaysFromNow)
        {
            booking.StartDate = DateTime.Today.AddDays(1);
            booking.EndDate = DateTime.Today.AddDays(endDaysFromNow);
        }

        [Given("no existing bookings")]
        public void GivenNoExistingBookings()
        {
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
        }

        [Given("all rooms are occupied during that period")]
        public void GivenAllRoomsAreOccupied()
        {
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, RoomId = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(10), IsActive = true },
                new Booking { Id = 2, RoomId = 2, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(10), IsActive = true },
                new Booking { Id = 3, RoomId = 3, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(10), IsActive = true },
            };
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);
        }

        // --- When steps ---

        [When("I try to create the booking")]
        public async Task WhenITryToCreateTheBooking()
        {
            try
            {
                createResult = await bookingManager.CreateBooking(booking);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
        }

        // --- Then steps ---

        [Then("an ArgumentException should be thrown")]
        public void ThenAnArgumentExceptionShouldBeThrown()
        {
            Assert.NotNull(caughtException);
            Assert.IsType<ArgumentException>(caughtException);
        }

        [Then("the booking should be created successfully")]
        public void ThenTheBookingShouldBeCreatedSuccessfully()
        {
            Assert.Null(caughtException);
            Assert.True(createResult);
        }

        [Then("the booking should not be created")]
        public void ThenTheBookingShouldNotBeCreated()
        {
            Assert.Null(caughtException);
            Assert.False(createResult);
        }
    }
}
