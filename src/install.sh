sudo cp ddnsservice /usr/bin/ddnsservice
sudo chmod 0755 /usr/bin/ddnsservice
sudo mkdir /etc/ddns
sudo cp ddns.conf /etc/ddns/ddns.conf
sudo cp duckdns.service /etc/systemd/system/duckdns.service
sudo systemctl daemon-reload
sudo systemctl enable duckdns
sudo systemctl start duckdns

