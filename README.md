# Trickler API

TODO: write readme

your trickles he steals

## Sample Requests

Create a trickle (POST request body example):

```json
{
	"title": "The Real Trickle",
	"text": "Solve the trickle and submit the correct answer to claim your reward",
	"answers": [
		{
			"answer": "Answer 1"
		},
		{
			"answer": "Answer 2"
		}
	],
	"availability": {
		"type": "DateRange",
		"from": {
			"year": 2026,
			"month": 4,
			"day": 2,
			"dayOfWeek": 4
		},
		"until": {
			"year": 2026,
			"month": 4,
			"day": 30,
			"dayOfWeek": 4
		},
		"dates": [
			{
				"year": 2026,
				"month": 4,
				"day": 10,
				"dayOfWeek": 6
			}
		],
		"daysOfWeek": [
			"Monday",
			"Wednesday",
			"Friday"
		]
	}
}
```
