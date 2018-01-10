FROM node:8.0.0

RUN apt-get update && \
    apt-get install -y tmux supervisor inotify-tools && \
    rm -rf /var/lib/apt/lists; rm /tmp/*; apt-get autoremove -y
