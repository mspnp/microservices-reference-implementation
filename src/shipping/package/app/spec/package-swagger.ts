const path = require('path');
const swaggerJSDoc = require('swagger-jsdoc');

const options = {
  definition: {
    openapi: '3.0.0',
    info: {
	  title: "fabrikam-drone-delivery-package-service",
	  description: "Fabrikam Drone Delivery Package Service",
	  version: "0.1.0",
	  contact: "Microsoft Patterns and Practices",
	  termsOfService: ''
	},
    basePath: '/api',
    "schemes": [
      "http",
      "https"
    ],
    "consumes": [
      "application/json"
    ],
    "produces": [
      "application/json"
    ]
  },
  apis: [path.join(__dirname, '../controllers/package-controllers.{ts,js}')],
};

export const PackageServiceSwaggerApi = swaggerJSDoc(options);
