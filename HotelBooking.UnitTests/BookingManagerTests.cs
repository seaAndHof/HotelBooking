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
        }

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
    }
}
