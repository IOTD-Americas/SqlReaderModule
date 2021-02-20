# IoT Edge Module that reads from Sql Server using a static sql query

This module use the module twin settings as follows:

```json
"SqlReaderModule": {
	"version": "1.0",
	"type": "docker",
	"status": "running",
	"restartPolicy": "always",
	"settings": {
		"image": "myacr.azurecr.io/SqlReaderModule.amd64",
		"createOptions": {}
	},
	"env": {
		"ConnectionString": {
		"value": ""
		},
		"SqlQuery": {
		"value": "SELECT top 1 * FROM MyTable"
		},
		"IsSqlQueryJson": {
		"value": "false"
		},
		"PoolingIntervalMiliseconds": {
		"value": "60000"
		},
		"MaxBatchSize": {
		"value": "200"
		},
		"Verbose": {
		"value": "true"
		}
	}
}
```

"IsSqlQueryJson": true --only used when query output is json, in any other case false

Roadmap improvements

 - Secure connection string
 - Support Stored Procedures
