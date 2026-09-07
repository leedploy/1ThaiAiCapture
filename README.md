# 🎯 1ThaiAi Capture - Windows Screen Capture Utility

โปรแกรม Windows Utility สำหรับจับภาพหน้าจอ (Screen Capture) ขนาดกะทัดรัด ทำงานเบื้องหลังใน System Tray พร้อมปุ่มลัด Global Hotkey **F1** และแถบเครื่องมือ Floating Toolbar สไตล์โมเดิร์น

พัฒนาด้วยภาษา **C# (Windows Forms)** ตัวเดียวจบในไฟล์เดียว (Single Form / Portable Executable)

---

## ✨ ฟังก์ชันและความสามารถหลัก

1. **ทำงานในเบื้องหลัง (Background & System Tray):**
   - เมื่อเปิดโปรแกรม จะซ่อนหน้าต่างหลักและไปรันอยู่ใน System Tray (มุมขวาล่างของจอ)
   - **คลิกซ้าย หรือ คลิกขวา** ที่ไอคอนใน Tray เพื่อเรียกเมนูตั้งค่าได้ทันที:
     - 📸 **จับภาพหน้าจอ (Capture)** / **Capture Screen**
     - ⚙️ **ตั้งค่าปุ่มลัด (Hotkey)** : เลือกปุ่มลัด F1, F2, F4, PrtScn, หรือ Ctrl+Shift+A
     - 📁 **ฟอร์แมตไฟล์ (Save Format)** : เลือกฟอร์แมตเริ่มต้น **PNG, JPG, หรือ BMP**
     - 🌐 **ภาษา (Language)** : สลับภาษา **🇹🇭 ไทย (Thai)** และ **🇬🇧 English** พร้อมไอคอนธงชาติ
     - 🌐 **1ThaiAi.com (ไปที่เว็บ)** : เปิดเว็บเบราว์เซอร์ไปที่ `https://1thaiai.com`
     - ❌ **Exit (ปิดโปรแกรม)** : คืนค่า Hotkey และออกจากโปรแกรม
   - ป้องกันการเปิดโปรแกรมซ้ำซ้อน (Single Instance Guard ด้วย Mutex)

2. **ดักจับปุ่มลัด Global Hotkey (F1):**
   - ใช้ระบบ **Low-Level Keyboard Hook (`WH_KEYBOARD_LL`)** ตัดสัญญาณคีย์ ป้องกันปุ่มลัดชนกับโปรแกรมอื่น
   - สามารถเลือกเปลี่ยนปุ่มลัดได้ง่ายๆ ผ่านเมนูที่ System Tray:
     - 🔘 **F1** (ค่าเริ่มต้น)
     - ⚪ **F2**
     - ⚪ **F4**
     - ⚪ **PrintScreen** (PrtScn)
     - ⚪ **Ctrl + Shift + A**

3. **โหมดจับภาพหน้าจอ (Capture Mode):**
   - เมื่อกด `F1` ระบบจะบันทึกภาพหน้าจอทั้งหมดทันที (รองรับหลายจอ Multi-Monitor / Virtual Screen)
   - แสดงหน้าต่าง Borderless TopMost Overlay เต็มจอ พร้อมเลเยอร์มืดโปร่งแสง (Dark Tint)
   - คลิกเมาส์ซ้ายค้างแล้วลาก (Drag to Select) เพื่อเลือกพื้นที่สี่เหลี่ยม
   - พื้นที่ที่เลือกจะสว่างคมชัด มีกรอบขอบเน้นสีฟ้าสไตล์โมเดิร์น และแสดงขนาดพิกเซลแบบ Real-time (`Width × Height px`)
   - **[✨ ใหม่] ปรับขนาดและย้ายตำแหน่งกรอบได้อย่างอิสระ (Interactive Resize & Move):**
     - สามารถดึงจุดแองเคอร์ทั้ง 8 จุด (มุมทั้ง 4 และขอบทั้ง 4 ด้าน) เพื่อย่อ/ขยายกรอบตามต้องการ
     - นำเมาส์ไปวางกลางกรอบเพื่อคลิกลากย้ายตำแหน่งกรอบทั้งกรอบได้อย่างอิสระ
     - ตัวชี้เมาส์ (Cursor) เปลี่ยนตามทิศทางและสถานะอย่างแม่นยำ
     - เส้นวาดและคำอธิบายประกอบ (Annotations) จะเคลื่อนย้ายตามกรอบไปด้วยอัตโนมัติ
   - กดปุ่ม **`Esc`** หรือคลิกขวาเพื่อยกเลิกได้ตลอดเวลา

4. **แถบเครื่องมือลอยคู่บน-ล่าง (Top & Bottom Floating Toolbars):**
   - **แถบด้านบน (Top Bar - เครื่องมือวาดเขียน):**
     - ✏️ **ปากกา (Pen)** : วาดเส้นอิสระสีแดงคมชัด
     - ➡️ **ลูกศร (Arrow)** : ลากเส้นลูกศรชี้เน้นตำแหน่ง
     - 🔲 **กรอบ (Box)** : ลากกรอบสี่เหลี่ยมตีกรอบข้อความ
     - ↩️ **ย้อนกลับ (Undo)** : ยกเลิกการวาดล่าสุด (หรือกด `Ctrl+Z`)
   - **แถบด้านล่าง (Bottom Bar - คำสั่งบันทึก/ใช้งาน):**
     - 📋 **คัดลอก (Copy)** : คัดลอกภาพไปยัง Clipboard (หรือกด `Enter`)
     - 💾 **บันทึก (Save)** : บันทึกภาพลงไฟล์ PNG/JPG/BMP
     - 🌐 **1ThaiAi** : เปิดเว็บไซต์ `https://1thaiai.com`
     - ✕ **ปิด (Close)** : ปิดหน้าต่างจับภาพ (หรือกด `Esc`)

5. **รองรับ High-DPI:**
   - คมชัดระดับพิกเซลบนจอความละเอียดสูง (Scaling 125%, 150%, 200%) ไม่เบลอและไม่คลาดเคลื่อน

---

## 🚀 วิธีสร้างไฟล์ .exe (Build Options)

### วิธีที่ 1: ดับเบิลคลิกคอมไพล์ทันทีด้วย `build.bat` หรือ `build.ps1` (แนะนำ - ไม่ต้องลงโปรแกรมเพิ่ม)
Windows ทุกเครื่องมีตัวคอมไพล์ C# (`csc.exe`) ติดมากับระบบอยู่แล้ว:
- ดับเบิลคลิกไฟล์ **`build.bat`** หรือรัน PowerShell `.\build.ps1`
- คุณจะได้ไฟล์ **`1ThaiAiCapture.exe`** (ขนาด ~21 KB) พร้อมใช้งานทันที

---

### วิธีที่ 2: เปิดและคอมไพล์ผ่าน Visual Studio (2019 / 2022 / 2025)
1. เปิดโปรแกรม **Visual Studio**
2. เลือก **Open a project or solution** -> เลือกไฟล์ `ThaiAiCapture.csproj`
3. กดปุ่ม **F5** หรือคลิกปุ่ม **Start (Green Arrow)** เพื่อทดสอบรันโปรแกรม
4. สั่ง Build โดยไปที่เมนู **Build** -> **Build Solution**

---

### วิธีที่ 3: Publish เป็น Single-File Executable (.NET 8 SDK)
หากติดตั้ง .NET 8 SDK ไว้ในเครื่อง สามารถใช้คำสั่ง CLI เพื่อสร้างไฟล์ `.exe` เดี่ยวที่รวม Runtime ไว้ในตัว (Self-contained):

```powershell
# คอมไพล์เป็น Single File Executable (รวม Runtime แบบ Standalone ไม่ต้องติดตั้ง .NET เพิ่ม)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ./publish
```

ไฟล์ `.exe` ที่ได้จะอยู่ในโฟลเดอร์ `publish/1ThaiAiCapture.exe`

---

## ⌨️ ปุ่มลัด (Hotkeys)

| ปุ่ม | คำสั่ง |
| :--- | :--- |
| **`F1`** | เริ่มต้นจับภาพหน้าจอ (Global Hotkey ทำงานจากทุกโปรแกรม) |
| **`ลากเมาส์ซ้าย`** | เลือกพื้นที่สี่เหลี่ยมที่ต้องการตัดภาพ |
| **`Enter`** | คัดลอกภาพที่เลือกไปยัง Clipboard ทันที |
| **`Esc`** หรือ **`คลิกขวา`** | ยกเลิกการจับภาพและปิด Overlay |

---

## 📂 โครงสร้างไฟล์โปรเจกต์

```text
e:\1ThaiAi Capture\
├── 1ThaiAiCapture.exe  # ไฟล์โปรแกรม Executable พร้อมใช้งาน (ฝังไอคอนในตัวเรียบร้อย)
├── app.ico             # ไฟล์ไอคอนความละเอียดสูง (256x256, 128x128, 64x64, 32x32, 16x16)
├── Program.cs          # ซอร์สโค้ดภาษา C# ทั้งหมด (Single Form / Architecture)
├── ThaiAiCapture.csproj# ไฟล์ Project สำหรับ Visual Studio & .NET SDK
├── app.manifest        # การตั้งค่า Per-Monitor DPI Aware และ Windows Support
├── build.bat           # สคริปต์คอมไพล์อัตโนมัติด้วย csc.exe ใน Windows
├── build.ps1           # สคริปต์คอมไพล์สำหรับ PowerShell
└── README.md           # คู่มือและเอกสารการใช้งาน
```
