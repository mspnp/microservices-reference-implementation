FROM node:8.12.0-alpine as base

WORKDIR /app

EXPOSE 80

# ---- install dependencies ----
FROM base AS dependencies

WORKDIR /app
COPY package.json .
COPY gulpfile.js .
RUN npm set progress=false && npm config set depth 0
RUN npm install --only=production
RUN cp -R node_modules prod_node_modules

# ---- build ----
FROM dependencies AS build
WORKDIR /app
RUN npm install
COPY tsconfig.json .
COPY app app/.
RUN  npm run build

# ---- runtime ----
FROM base AS runtime

MAINTAINER Fernando Antivero (https://github.com/ferantivero)

WORKDIR /app
COPY --from=dependencies /app/prod_node_modules ./node_modules
COPY --from=build /app/.bin/app .

ENTRYPOINT ["node", "main.js"]
