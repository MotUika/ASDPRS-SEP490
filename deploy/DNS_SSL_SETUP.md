# 🌐 Hướng dẫn Setup DNS và SSL cho Domain

**VPS IP:** 160.25.232.199  
**Mục đích:** Trỏ domain về VPS và cài SSL certificate

---

## 📝 Bước 1: Cấu hình DNS Records

### Option A: Root Domain (yourdomain.com)

Đăng nhập vào nhà cung cấp domain của bạn và thêm DNS records:

| Type | Name/Host | Value/Points to | TTL |
|------|-----------|-----------------|-----|
| **A** | @ | 160.25.232.199 | 3600 |
| **A** | www | 160.25.232.199 | 3600 |

### Option B: Subdomain (api.yourdomain.com)

| Type | Name/Host | Value/Points to | TTL |
|------|-----------|-----------------|-----|
| **A** | api | 160.25.232.199 | 3600 |

---

## 🏢 Hướng dẫn theo nhà cung cấp

### 1️⃣ **GoDaddy**
1. Login vào https://account.godaddy.com
2. Vào **My Products** → **Domains** → Click domain của bạn
3. Click **DNS** → **Manage Zones**
4. Click **Add** để thêm record:
   - Type: **A**
   - Name: **@** (hoặc **www**, **api**)
   - Value: **160.25.232.199**
   - TTL: **1 Hour**
5. Click **Save**

### 2️⃣ **Namecheap**
1. Login vào https://www.namecheap.com
2. Vào **Domain List** → Click **Manage** bên cạnh domain
3. Vào tab **Advanced DNS**
4. Click **Add New Record**:
   - Type: **A Record**
   - Host: **@** (hoặc **www**, **api**)
   - Value: **160.25.232.199**
   - TTL: **Automatic**
5. Click ✓ (Save)

### 3️⃣ **Cloudflare** (Khuyến nghị - có CDN miễn phí)
1. Login vào https://dash.cloudflare.com
2. Chọn domain của bạn
3. Vào tab **DNS** → **Records**
4. Click **Add record**:
   - Type: **A**
   - Name: **@** (hoặc **www**, **api**)
   - IPv4 address: **160.25.232.199**
   - Proxy status: **Proxied** 🟠 (để bật CDN) hoặc **DNS only** (không CDN)
   - TTL: **Auto**
5. Click **Save**

**Lưu ý Cloudflare:**
- **Proxied (🟠)**: Traffic đi qua Cloudflare CDN, có HTTPS tự động, DDoS protection
- **DNS only**: Trỏ thẳng về VPS, cần tự cài SSL

### 4️⃣ **Google Domains / Squarespace**
1. Login vào https://domains.google.com (hoặc Squarespace)
2. Click domain → **DNS**
3. Scroll xuống **Custom records**
4. Click **Manage custom records** → **Create new record**:
   - Host name: **@** (hoặc **www**, **api**)
   - Type: **A**
   - Data: **160.25.232.199**
   - TTL: **3600**
5. Click **Save**

### 5️⃣ **Nhà cung cấp Việt Nam (PA, INET, TenCuaBan, etc.)**
1. Login vào trang quản lý domain
2. Tìm mục **Quản lý DNS** / **DNS Management**
3. Thêm bản ghi:
   - Loại: **A**
   - Tên: **@** (hoặc **www**, **api**)
   - Giá trị / IP: **160.25.232.199**
   - TTL: **3600**
4. Lưu lại

---

## ⏱️ Bước 2: Đợi DNS Propagate

DNS thường mất **5 phút - 48 giờ** để cập nhật toàn cầu (thường là 15-30 phút).

### Kiểm tra DNS đã trỏ đúng chưa:

#### **Cách 1: Dùng nslookup (Windows)**
```cmd
nslookup yourdomain.com
```

Kết quả mong đợi:
```
Non-authoritative answer:
Name:    yourdomain.com
Address: 160.25.232.199
```

#### **Cách 2: Dùng dig (Linux/Mac)**
```bash
dig yourdomain.com +short
```

Kết quả mong đợi:
```
160.25.232.199
```

#### **Cách 3: Online Tools**
- https://dnschecker.org
- https://www.whatsmydns.net
- Nhập domain và check xem IP có phải `160.25.232.199` không

---

## 🔒 Bước 3: Cài SSL Certificate (Let's Encrypt - FREE)

**SAU KHI** DNS đã trỏ đúng (check ở bước 2), SSH vào VPS và chạy:

```bash
ssh root@160.25.232.199

cd /opt/asdprs/deploy/scripts

# Thay 'yourdomain.com' bằng domain thật của bạn
./05-ssl-setup.sh yourdomain.com
```

### Script sẽ tự động:
1. ✅ Cài đặt Certbot
2. ✅ Generate SSL certificate từ Let's Encrypt
3. ✅ Cấu hình Nginx cho HTTPS
4. ✅ Setup auto-renewal (tự động gia hạn mỗi 3 tháng)
5. ✅ Reload Nginx

### Sau khi SSL setup xong:
- ✅ **HTTP:** http://yourdomain.com → tự động redirect sang HTTPS
- ✅ **HTTPS:** https://yourdomain.com → API của bạn
- ✅ **Swagger:** https://yourdomain.com/swagger

---

## 🔍 Kiểm tra SSL hoạt động

### 1. Truy cập HTTPS
```
https://yourdomain.com
https://yourdomain.com/swagger
```

### 2. Check SSL Certificate
```bash
# Linux/Mac
curl -vI https://yourdomain.com 2>&1 | grep -i "issuer"

# Windows PowerShell
(Invoke-WebRequest -Uri "https://yourdomain.com").BaseResponse.Certificate | fl
```

### 3. Online SSL Checker
- https://www.ssllabs.com/ssltest
- Nhập domain và check rating (mục tiêu: A hoặc A+)

---

## 📊 Ví dụ DNS Setup Hoàn Chỉnh

### Ví dụ: Domain là `example.com`

#### DNS Records:
```
Type    Name    Value               TTL
A       @       160.25.232.199     3600
A       www     160.25.232.199     3600
A       api     160.25.232.199     3600
```

#### Sau khi DNS propagate:
```bash
# Check DNS
nslookup example.com
nslookup www.example.com
nslookup api.example.com
# → Tất cả đều trả về: 160.25.232.199
```

#### Cài SSL:
```bash
ssh root@160.25.232.199
cd /opt/asdprs/deploy/scripts

# Cài SSL cho cả 3 domains cùng lúc
./05-ssl-setup.sh example.com www.example.com api.example.com

# Hoặc từng cái một
./05-ssl-setup.sh example.com
./05-ssl-setup.sh www.example.com
./05-ssl-setup.sh api.example.com
```

#### URLs sau khi setup:
- ✅ https://example.com
- ✅ https://www.example.com
- ✅ https://api.example.com
- ✅ https://example.com/swagger

---

## 🚨 Troubleshooting

### ❌ DNS không trỏ được

**Triệu chứng:** `nslookup yourdomain.com` không trả về `160.25.232.199`

**Giải pháp:**
1. Kiểm tra lại DNS records đã save chưa
2. Đợi thêm thời gian (DNS propagation)
3. Clear DNS cache:
   ```cmd
   # Windows
   ipconfig /flushdns
   
   # Mac
   sudo dscacheutil -flushcache
   
   # Linux
   sudo systemd-resolve --flush-caches
   ```
4. Thử dùng DNS khác: `nslookup yourdomain.com 8.8.8.8`

### ❌ SSL setup lỗi "Failed to verify domain"

**Nguyên nhân:** DNS chưa trỏ đúng hoặc port 80/443 bị block

**Giải pháp:**
1. Đảm bảo DNS đã trỏ đúng (check bằng `nslookup`)
2. Check firewall:
   ```bash
   ufw status
   ufw allow 80/tcp
   ufw allow 443/tcp
   ```
3. Đảm bảo Nginx đang chạy:
   ```bash
   asdprs status
   ```
4. Thử lại sau 15-30 phút

### ❌ Website hiện "Not Secure" hoặc SSL error

**Giải pháp:**
```bash
# Check nginx config
nginx -t

# Restart nginx
docker restart asdprs-nginx

# Check SSL certificate
certbot certificates

# Renew SSL
certbot renew --force-renewal
docker restart asdprs-nginx
```

### ❌ Cloudflare "Too many redirects"

**Nguyên nhân:** Cloudflare SSL/TLS mode không đúng

**Giải pháp:**
1. Vào Cloudflare Dashboard → **SSL/TLS**
2. Chọn mode: **Full** hoặc **Full (strict)**
3. Đợi vài phút để cập nhật

---

## 📋 Checklist Setup Domain

- [ ] 1. Tạo DNS A record trỏ về `160.25.232.199`
- [ ] 2. Đợi DNS propagate (15-30 phút)
- [ ] 3. Verify DNS bằng `nslookup` hoặc online tool
- [ ] 4. SSH vào VPS
- [ ] 5. Chạy `./05-ssl-setup.sh yourdomain.com`
- [ ] 6. Đợi SSL certificate được tạo (1-2 phút)
- [ ] 7. Test HTTPS: `https://yourdomain.com`
- [ ] 8. Test Swagger: `https://yourdomain.com/swagger`
- [ ] 9. Check SSL rating tại ssllabs.com
- [ ] 10. Update `appsettings.Production.json` với domain mới (nếu cần)

---

## 🎯 Cấu hình nâng cao

### Thêm subdomain cho môi trường khác nhau:

| Subdomain | Purpose | DNS Record |
|-----------|---------|------------|
| api.domain.com | Production API | A → 160.25.232.199 |
| staging.domain.com | Staging | A → [IP khác] |
| dev.domain.com | Development | A → [IP khác] |
| admin.domain.com | Admin Panel | A → 160.25.232.199 |

### Redirect www sang non-www (hoặc ngược lại):

Sau khi SSL setup xong, edit nginx config nếu cần:
```bash
nano /opt/asdprs/deploy/nginx.conf
```

Thêm redirect block:
```nginx
server {
    listen 443 ssl;
    server_name www.yourdomain.com;
    
    # Redirect www to non-www
    return 301 https://yourdomain.com$request_uri;
}
```

Reload nginx:
```bash
docker restart asdprs-nginx
```

---

## 🔄 Auto-renewal SSL

SSL Certificate sẽ **tự động gia hạn** mỗi 3 tháng nhờ Certbot.

### Kiểm tra auto-renewal:
```bash
# Check cron job
crontab -l | grep certbot

# Test renewal (không thật sự renew)
certbot renew --dry-run

# Manual renew nếu cần
certbot renew
docker restart asdprs-nginx
```

---

## 📞 Tóm tắt quy trình

```
1. Mua domain
   ↓
2. Vào quản lý DNS
   ↓
3. Thêm A record: @ → 160.25.232.199
   ↓
4. Đợi 15-30 phút (DNS propagate)
   ↓
5. Check: nslookup yourdomain.com
   ↓
6. SSH vào VPS: ssh root@160.25.232.199
   ↓
7. Run: ./05-ssl-setup.sh yourdomain.com
   ↓
8. Đợi 1-2 phút
   ↓
9. Truy cập: https://yourdomain.com/swagger
   ↓
10. ✅ DONE!
```

---

## 💡 Tips

1. **Dùng Cloudflare** (miễn phí) để có:
   - CDN tự động
   - DDoS protection
   - Analytics
   - Flexible SSL (không cần cài SSL trên VPS nếu chọn Flexible mode)

2. **Subdomain cho API:** Nên dùng `api.domain.com` thay vì root domain

3. **Monitoring:** Setup uptime monitoring:
   - https://uptimerobot.com (miễn phí)
   - https://www.pingdom.com

4. **Backup DNS:** Note lại DNS records để phòng trường hợp cần restore

---

**Chúc bạn setup thành công! 🚀**
