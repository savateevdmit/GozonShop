FROM nginx:1.27
COPY frontend/Web.Ui/index.html /usr/share/nginx/html/index.html
COPY frontend/Web.Ui/styles.css /usr/share/nginx/html/styles.css
COPY frontend/Web.Ui/app.js /usr/share/nginx/html/app.js
COPY frontend/Web.Ui/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80