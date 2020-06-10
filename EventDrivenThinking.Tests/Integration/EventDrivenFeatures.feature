Feature: EventDrivenFeatures
	In order to siplify development I want to show development process

#@app
Scenario: I want to execute simple command against an aggregate
	
	Given The fact: Room was added:
	| Property | Value |
	| Number   | 101   |
	
	When I query for availability of a room:
	| Parameter | Value      |
	| Start     | 2020-03-03 |
	| End       | 2020-03-04 |
	| Floor     | 1          |
	
	Then I get query results: 
	| Property       | Value   |
	| AvailableRooms | ["101"] |
	| Found          | true    |

	When I book a room:
	| Property | Value      |
	| Number   | 101        |
	| Start    | 2020-03-03 |
	| End      | 2020-03-04 |
	

	Then I expect that room was booked:
	| Property | Value      |
	| Number   | 101        |
	| Start    | 2020-03-03 |
	| End      | 2020-03-04 |

	And I get query results:
	| Property       | Value |
	| AvailableRooms | []    |
	| Found          | false |

