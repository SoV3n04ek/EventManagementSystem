# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Reach into the specific frontend folder for the lockfile
COPY frontend/event-management-app/package*.json ./
RUN npm ci

# Copy the rest of the frontend source
COPY frontend/event-management-app/ .

# Build for production
RUN npm run build -- --configuration production

# Stage 2: Serve with Nginx
FROM nginx:alpine

# The rest remains the same...
COPY --from=build /app/dist/event-management-app/browser /usr/share/nginx/html
COPY frontend/event-management-app/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]