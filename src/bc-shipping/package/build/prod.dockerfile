FROM node:8.0.0

WORKDIR /app
EXPOSE 80

COPY package.json /app/
RUN npm install --production

COPY .bin/app /app

ENTRYPOINT ["node", "main.js"]
