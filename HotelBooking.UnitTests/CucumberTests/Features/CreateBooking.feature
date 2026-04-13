Feature: Create Booking
  As a hotel manager
  I want to create bookings for hotel rooms
  So that customers can reserve rooms for their stay

  Background:
    Given the hotel has rooms available

  Scenario: Booking with start date today should fail
    Given the start date is today and end date is 5 days from now
    When I try to create the booking
    Then an ArgumentException should be thrown

  Scenario: Booking with end date before start date should fail
    Given the start date is 5 days from now and end date is 2 days from now
    When I try to create the booking
    Then an ArgumentException should be thrown

  Scenario: Booking with valid dates and available room should succeed
    Given the start date is tomorrow and end date is 5 days from now
    And no existing bookings
    When I try to create the booking
    Then the booking should be created successfully

  Scenario: Booking with valid dates but all rooms occupied should fail
    Given the start date is tomorrow and end date is 5 days from now
    And all rooms are occupied during that period
    When I try to create the booking
    Then the booking should not be created
