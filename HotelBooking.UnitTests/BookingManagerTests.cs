using System;
using System.Collections.Generic;
using HotelBooking.Core;
using Xunit;
using System.Threading.Tasks;
using Moq;


namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private readonly IBookingManager bookingManager;
        private readonly Mock<IRepository<Booking>> bookingRepositoryMock;

        public BookingManagerTests()
        {
            bookingRepositoryMock = new Mock<IRepository<Booking>>();
            var roomRepositoryMock = new Mock<IRepository<Room>>();

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "A" },
                new Room { Id = 2, Description = "B" },
                new Room { Id = 3, Description = "C" },
            };

            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rooms);
            bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);
        }

        #region Test Data Setup

        public static TheoryData<DateTime, DateTime> InvalidDateTestData
        {
            get
            {
                var data = new TheoryData<DateTime, DateTime>
                {
                    { DateTime.Today, DateTime.Today.AddDays(1) },
                    { DateTime.Today.AddDays(2), DateTime.Today.AddDays(1) }
                };
                return data;
            }
        }

        public static TheoryData<List<Booking>> UnavailableRoomTestData
        {
            get
            {
                var data = new TheoryData<List<Booking>>
                {
                    new List<Booking>
                    {
                        new Booking { Id = 1, RoomId = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = true },
                        new Booking { Id = 2, RoomId = 2, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = true },
                        new Booking { Id = 3, RoomId = 3, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = true }
                    }
                };
                return data;
            }
        }

        public static TheoryData<List<Booking>, int> AvailableRoomTestData
        {
            get
            {
                var data = new TheoryData<List<Booking>, int>();
                data.Add(new List<Booking>(), 1);
                data.Add(new List<Booking>
                {
                    new Booking { Id = 1, RoomId = 1, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(12), IsActive = true },
                    new Booking { Id = 2, RoomId = 2, StartDate = DateTime.Today.AddDays(13), EndDate = DateTime.Today.AddDays(18), IsActive = true }
                }, 3);
                return data;
            }
        }
        
        #endregion

        #region FindAvailableRoom
        
        [Theory]
        [MemberData(nameof(InvalidDateTestData))]
        public async Task FindAvailableRoom_InvalidDates_ThrowsArgumentException(DateTime startDate, DateTime endDate)
        {
            // Arrange
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Booking>());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => bookingManager.FindAvailableRoom(startDate, endDate));
        }

        [Theory]
        [MemberData(nameof(AvailableRoomTestData))]
        public async Task FindAvailableRoom_RoomIsAvailable_ReturnsRoomId(List<Booking> bookings, int expectedRoomId)
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(15);
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(expectedRoomId, roomId);
        }

        [Theory]
        [MemberData(nameof(UnavailableRoomTestData))]
        public async Task FindAvailableRoom_RoomIsUnavailable_ReturnsMinusOne(List<Booking> bookings)
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(5);
            DateTime endDate = DateTime.Today.AddDays(6);
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId);
        }

        #endregion
    }
}
