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
        private readonly Mock<IRepository<Room>> roomRepositoryMock;
        
        public BookingManagerTests()
        {
            bookingRepositoryMock = new Mock<IRepository<Booking>>();
            roomRepositoryMock = new Mock<IRepository<Room>>();
            bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);
            
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

        #region Get Fully Occupied Dates
        
        [Fact]
        public async Task GetFullyOccupiedDates_InvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
            DateTime startDate = new DateTime(2026, 2, 10);
            DateTime endDate = new DateTime(2026, 2, 5);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                bookingManager.GetFullyOccupiedDates(startDate, endDate));
        }

        [Theory]
        [MemberData(nameof(GetFullyOccupiedDatesTestData))]
        public async Task GetFullyOccupiedDates_ValidDateRange_ReturnsExpectedDates(
            List<Room> rooms, List<Booking> bookings, DateTime startDate, DateTime endDate, List<DateTime> expectedDates)
        {
            // Arrange
            bookingRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings);
            roomRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(rooms);

            // Act
            var result = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(expectedDates, result);
        }

        public static TheoryData<List<Room>, List<Booking>, DateTime, DateTime, List<DateTime>> GetFullyOccupiedDatesTestData()
        {
            var data = new TheoryData<List<Room>, List<Booking>, DateTime, DateTime, List<DateTime>>();

            // Scenario 1: No rooms, no bookings
            data.Add(
                [],
                [],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                []
            );

            // Scenario 2: Some rooms, no bookings
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                []
            );

            // Scenario 3: Single day range, no bookings
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" }
                ],
                [],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 1),
                []
            );

            // Scenario 4: Single day range, fully occupied
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 1), IsActive = true, CustomerId = 1 },
                    new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 1), IsActive = true, CustomerId = 2 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 1),
                [new DateTime(2026, 2, 1)]
            );

            // Scenario 5: Single day range, partially occupied
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 1), IsActive = true, CustomerId = 1 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 1),
                []
            );

            // Scenario 6: Multi-day range, no bookings
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" }
                ],
                [],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                []
            );

            // Scenario 7: Multi-day range, some days fully occupied, some not
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 2), IsActive = true, CustomerId = 1 },
                    new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 2), IsActive = true, CustomerId = 2 },
                    new() { Id = 3, RoomId = 1, StartDate = new DateTime(2026, 2, 4), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 3 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                [
                    new(2026, 2, 1), 
                    new(2026, 2, 2)
                ]
            );

            // Scenario 8: Multi-day range, all days fully occupied
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 1 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                [
                    new(2026, 2, 1),
                    new(2026, 2, 2),
                    new(2026, 2, 3),
                    new(2026, 2, 4),
                    new(2026, 2, 5)
                ]
            );

            // Scenario 9: Booking starts before range, ends during range
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 1, 28), EndDate = new DateTime(2026, 2, 3), IsActive = true, CustomerId = 1 },
                    new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 1, 28), EndDate = new DateTime(2026, 2, 3), IsActive = true, CustomerId = 2 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                [
                    new(2026, 2, 1), 
                    new(2026, 2, 2), 
                    new(2026, 2, 3)
                ]
            );

            // Scenario 10: Booking starts during range, ends after range
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 10), IsActive = true, CustomerId = 1 },
                    new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 10), IsActive = true, CustomerId = 2 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                [
                    new(2026, 2, 3), 
                    new(2026, 2, 4), 
                    new(2026, 2, 5)
                ]
            );

            // Scenario 11: Mix of active and inactive bookings
            data.Add(
                [
                    new() { Id = 1, Description = "Room 1" },
                    new() { Id = 2, Description = "Room 2" }
                ],
                [
                    new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 1 },
                    new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 5), IsActive = false, CustomerId = 2 },
                    new() { Id = 3, RoomId = 2, StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 3 }
                ],
                new DateTime(2026, 2, 1),
                new DateTime(2026, 2, 5),
                [
                    new(2026, 2, 3), 
                    new(2026, 2, 4), 
                    new(2026, 2, 5)
                ]
            );

            return data;
        }
        

        #endregion

        #region CreateBooking

        [Fact]
        public async Task CreateBooking_RoomAvailable_ReturnsTrue()
        {
            // Arrange
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Booking>());
            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(15),
                CustomerId = 1
            };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateBooking_NoRoomAvailable_ReturnsFalse()
        {
            // Arrange
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, RoomId = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(20), IsActive = true },
                new Booking { Id = 2, RoomId = 2, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(20), IsActive = true },
                new Booking { Id = 3, RoomId = 3, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(20), IsActive = true }
            };
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(bookings);
            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(15),
                CustomerId = 1
            };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(InvalidDateTestData))]
        public async Task CreateBooking_InvalidDates_ThrowsArgumentException(DateTime startDate, DateTime endDate)
        {
            // Arrange
            var booking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = 1
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => bookingManager.CreateBooking(booking));
        }

        #endregion
    }
}
