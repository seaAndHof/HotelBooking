using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class GetFullyOccupiedDatesTests
    {

        [Fact]
        public async Task GetFullyOccupiedDates_InvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
            var mockBookingRepo = new Mock<IRepository<Booking>>();
            var mockRoomRepo = new Mock<IRepository<Room>>();
            var bookingManager = new BookingManager(mockBookingRepo.Object, mockRoomRepo.Object);
            
            DateTime startDate = new DateTime(2026, 2, 10);
            DateTime endDate = new DateTime(2026, 2, 5);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                bookingManager.GetFullyOccupiedDates(startDate, endDate));
        }

        [Theory]
        [MemberData(nameof(GetFullyOccupiedDatesTestData))]
        public async Task GetFullyOccupiedDates_ValidDateRange_ReturnsExpectedDates(
            GetFullyOccupiedDatesTestCase testCase)
        {
            // Arrange
            var mockBookingRepo = new Mock<IRepository<Booking>>();
            var mockRoomRepo = new Mock<IRepository<Room>>();
            
            mockBookingRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(testCase.Bookings);
            mockRoomRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(testCase.Rooms);
            
            var bookingManager = new BookingManager(mockBookingRepo.Object, mockRoomRepo.Object);

            // Act
            var result = await bookingManager.GetFullyOccupiedDates(testCase.StartDate, testCase.EndDate);

            // Assert
            Assert.Equal(testCase.ExpectedDates, result);
        }
        
        public record GetFullyOccupiedDatesTestCase
        {
            public required List<Room> Rooms { get; init; }
            public required List<Booking> Bookings { get; init; }
            public required DateTime StartDate { get; init; }
            public required DateTime EndDate { get; init; }
            public required List<DateTime> ExpectedDates { get; init; }
        }

        public static IEnumerable<GetFullyOccupiedDatesTestCase[]> GetFullyOccupiedDatesTestData()
        {
            // Scenario 1: No rooms, no bookings
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [],
                    Bookings = [],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = []
                }
            ];

            // Scenario 2: Some rooms, no bookings
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = []
                }
            ];

            // Scenario 3: Single day range, no bookings
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" }
                    ],
                    Bookings = [],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 1),
                    ExpectedDates = []
                }
            ];

            // Scenario 4: Single day range, fully occupied
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 1), IsActive = true, CustomerId = 1 },
                        new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 1), IsActive = true, CustomerId = 2 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 1),
                    ExpectedDates = [new DateTime(2026, 2, 1)]
                }
            ];

            // Scenario 5: Single day range, partially occupied
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 1), IsActive = true, CustomerId = 1 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 1),
                    ExpectedDates = []
                }
            ];

            // Scenario 6: Multi-day range, no bookings
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" }
                    ],
                    Bookings = [],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = []
                }
            ];

            // Scenario 7: Multi-day range, some days fully occupied, some not
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 2), IsActive = true, CustomerId = 1 },
                        new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 2), IsActive = true, CustomerId = 2 },
                        new() { Id = 3, RoomId = 1, StartDate = new DateTime(2026, 2, 4), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 3 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = [
                        new(2026, 2, 1), 
                        new(2026, 2, 2)
                    ]
                }
            ];

            // Scenario 8: Multi-day range, all days fully occupied
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 1 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = [
                        new(2026, 2, 1),
                        new(2026, 2, 2),
                        new(2026, 2, 3),
                        new(2026, 2, 4),
                        new(2026, 2, 5)
                    ]
                }
            ];

            // Scenario 9: Booking starts before range, ends during range
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 1, 28), EndDate = new DateTime(2026, 2, 3), IsActive = true, CustomerId = 1 },
                        new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 1, 28), EndDate = new DateTime(2026, 2, 3), IsActive = true, CustomerId = 2 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = [
                        new(2026, 2, 1), 
                        new(2026, 2, 2), 
                        new(2026, 2, 3)
                    ]
                }
            ];

            // Scenario 10: Booking starts during range, ends after range
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 10), IsActive = true, CustomerId = 1 },
                        new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 10), IsActive = true, CustomerId = 2 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = [
                        new(2026, 2, 3), 
                        new(2026, 2, 4), 
                        new(2026, 2, 5)
                    ]
                }
            ];

            // Scenario 11: Mix of active and inactive bookings
            yield return
            [
                new GetFullyOccupiedDatesTestCase
                {
                    Rooms = [
                        new() { Id = 1, Description = "Room 1" },
                        new() { Id = 2, Description = "Room 2" }
                    ],
                    Bookings = [
                        new() { Id = 1, RoomId = 1, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 1 },
                        new() { Id = 2, RoomId = 2, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 5), IsActive = false, CustomerId = 2 },
                        new() { Id = 3, RoomId = 2, StartDate = new DateTime(2026, 2, 3), EndDate = new DateTime(2026, 2, 5), IsActive = true, CustomerId = 3 }
                    ],
                    StartDate = new DateTime(2026, 2, 1),
                    EndDate = new DateTime(2026, 2, 5),
                    ExpectedDates = [
                        new(2026, 2, 3), 
                        new(2026, 2, 4), 
                        new(2026, 2, 5)
                    ]
                }
            ];
        }
    }
}

