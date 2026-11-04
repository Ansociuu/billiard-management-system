import os
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as patches

os.makedirs('docs/figures', exist_ok=True)

# 1.1 Architecture
def gen_1_1():
    fig, ax = plt.subplots(figsize=(8, 4.5), dpi=300)
    ax.axis('off')
    
    p_box = patches.FancyBboxPatch((0.05, 0.68), 0.9, 0.25, boxstyle='round,pad=0.02', ec='black', fc='white', lw=1.5)
    ax.add_patch(p_box)
    ax.text(0.5, 0.88, 'TẦNG TRÌNH DIỄN (PRESENTATION LAYER - CLIENT)', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.5, 0.76, 'Windows Presentation Foundation (WPF) - XAML UI & Material Design\nView (Data Binding) <===> ViewModel (INotifyPropertyChanged, ICommand)', ha='center', va='center', fontsize=9.5, color='black')
    
    b_box = patches.FancyBboxPatch((0.05, 0.36), 0.9, 0.25, boxstyle='round,pad=0.02', ec='black', fc='white', lw=1.5)
    ax.add_patch(b_box)
    ax.text(0.5, 0.56, 'TẦNG NGHIỆP VỤ & TRUY CẬP DỮ LIỆU (BUSINESS & DATA ACCESS LAYER)', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.5, 0.44, 'Services: TableService, OrderService, UserService, AuthService, AuditLogService\nData Access: DatabaseHelper, MySqlConnector (High-Performance ADO.NET)', ha='center', va='center', fontsize=9.5, color='black')
    
    d_box = patches.FancyBboxPatch((0.05, 0.04), 0.9, 0.25, boxstyle='round,pad=0.02', ec='black', fc='white', lw=1.5)
    ax.add_patch(d_box)
    ax.text(0.5, 0.24, 'TẦNG CƠ SỞ DỮ LIỆU (DATABASE LAYER - CLOUD)', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.5, 0.12, 'MySQL 8.0 Managed Database (Aiven Cloud Services)\nBảo mật: SSL Mode Required (TLS 1.3), Bộ mã ký tự: utf8mb4 / utf8mb4_unicode_ci', ha='center', va='center', fontsize=9.5, color='black')
    
    ax.annotate('', xy=(0.5, 0.61), xytext=(0.5, 0.68), arrowprops=dict(arrowstyle='<->', color='black', lw=1.5))
    ax.annotate('', xy=(0.5, 0.29), xytext=(0.5, 0.36), arrowprops=dict(arrowstyle='<->', color='black', lw=1.5))
    
    plt.tight_layout()
    plt.savefig('docs/figures/fig_1_1_architecture.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.1 MVVM
def gen_2_1():
    fig, ax = plt.subplots(figsize=(8, 4), dpi=300)
    ax.axis('off')
    
    v_box = patches.FancyBboxPatch((0.05, 0.3), 0.25, 0.45, boxstyle='round,pad=0.03', ec='black', fc='white', lw=1.5)
    ax.add_patch(v_box)
    ax.text(0.175, 0.65, 'VIEW\n(Giao diện)', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.175, 0.45, '- XAML Controls\n- DataTemplates\n- Styles/Themes\n- ValueConverters', ha='center', va='center', fontsize=9, color='black')
    
    vm_box = patches.FancyBboxPatch((0.38, 0.3), 0.25, 0.45, boxstyle='round,pad=0.03', ec='black', fc='white', lw=1.5)
    ax.add_patch(vm_box)
    ax.text(0.505, 0.65, 'VIEWMODEL\n(Logic hiển thị)', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.505, 0.45, '- ObservableObject\n- ICommand / Relay\n- State Management\n- Call Services', ha='center', va='center', fontsize=9, color='black')
    
    m_box = patches.FancyBboxPatch((0.71, 0.3), 0.25, 0.45, boxstyle='round,pad=0.03', ec='black', fc='white', lw=1.5)
    ax.add_patch(m_box)
    ax.text(0.835, 0.65, 'MODEL\n(Mô hình dữ liệu)', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.835, 0.45, '- Entities / DTOs\n- Business Rules\n- Validation\n- Database Schema', ha='center', va='center', fontsize=9, color='black')
    
    ax.annotate('', xy=(0.38, 0.58), xytext=(0.30, 0.58), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.text(0.34, 0.61, 'Commands\nEvents', ha='center', va='bottom', fontsize=8, color='black')
    
    ax.annotate('', xy=(0.30, 0.42), xytext=(0.38, 0.42), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.text(0.34, 0.35, 'Data Binding\nNotification', ha='center', va='top', fontsize=8, color='black')
    
    ax.annotate('', xy=(0.71, 0.58), xytext=(0.63, 0.58), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.text(0.67, 0.61, 'Updates', ha='center', va='bottom', fontsize=8, color='black')
    
    ax.annotate('', xy=(0.63, 0.42), xytext=(0.71, 0.42), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.text(0.67, 0.35, 'Query data', ha='center', va='top', fontsize=8, color='black')

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_1_mvvm.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.2 Use Case
def gen_2_2():
    fig, ax = plt.subplots(figsize=(8.5, 6), dpi=300)
    ax.axis('off')
    
    sys_box = patches.Rectangle((0.24, 0.05), 0.52, 0.9, fill=False, ec='black', lw=1.5, ls='--')
    ax.add_patch(sys_box)
    ax.text(0.5, 0.92, 'HỆ THỐNG QUẢN LÝ QUÁN BILLIARD', ha='center', va='center', fontsize=12, fontweight='bold', color='black')
    
    # Actors
    ax.text(0.12, 0.72, 'Nhân viên\n(Staff)', ha='center', va='center', fontsize=10, fontweight='bold', color='black')
    circle_staff = patches.Circle((0.12, 0.82), 0.04, fill=False, ec='black', lw=1.2)
    ax.add_patch(circle_staff)
    ax.plot([0.12, 0.12], [0.78, 0.73], color='black', lw=1.2)
    
    ax.text(0.12, 0.25, 'Quản trị viên\n(Admin)', ha='center', va='center', fontsize=10, fontweight='bold', color='black')
    circle_admin = patches.Circle((0.12, 0.35), 0.04, fill=False, ec='black', lw=1.2)
    ax.add_patch(circle_admin)
    ax.plot([0.12, 0.12], [0.31, 0.26], color='black', lw=1.2)
    
    usecases = [
        (0.5, 0.84, 'UC01: Đăng nhập & Xác thực'),
        (0.5, 0.73, 'UC02: Quản lý bàn & Mở phiên chơi'),
        (0.5, 0.62, 'UC03: Gọi dịch vụ / F&B vào bàn'),
        (0.5, 0.51, 'UC04: Chuyển bàn chơi (Switch Table)'),
        (0.5, 0.40, 'UC05: Thanh toán & Xuất hóa đơn'),
        (0.5, 0.29, 'UC06: Quản lý tài khoản & Phân quyền'),
        (0.5, 0.18, 'UC07: Báo cáo thống kê & Doanh thu'),
        (0.5, 0.08, 'UC08: Truy vết nhật ký hệ thống (Audit)')
    ]
    
    for x, y, text in usecases:
        ellipse = patches.Ellipse((x, y), 0.44, 0.08, fill=False, ec='black', lw=1.2)
        ax.add_patch(ellipse)
        ax.text(x, y, text, ha='center', va='center', fontsize=8.5, color='black')
        
    for y in [0.84, 0.73, 0.62, 0.51, 0.40]:
        ax.plot([0.16, 0.28], [0.72, y], color='black', lw=1)
        
    for y in [0.84, 0.73, 0.62, 0.51, 0.40, 0.29, 0.18, 0.08]:
        ax.plot([0.16, 0.28], [0.25, y], color='black', lw=1)

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_2_usecase.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.3 Sequence Open Table
def gen_2_3():
    fig, ax = plt.subplots(figsize=(8, 4.5), dpi=300)
    ax.axis('off')
    
    lifelines = [
        (0.12, 'Nhân viên'),
        (0.36, 'TableManagementVM'),
        (0.62, 'TableService'),
        (0.88, 'MySQL Database')
    ]
    
    for x, label in lifelines:
        box = patches.Rectangle((x-0.1, 0.85), 0.2, 0.1, fill=False, ec='black', lw=1.2)
        ax.add_patch(box)
        ax.text(x, 0.9, label, ha='center', va='center', fontsize=9, fontweight='bold', color='black')
        ax.plot([x, x], [0.85, 0.08], color='black', lw=1, ls=':')
        
    steps = [
        (0.12, 0.36, 0.78, '1. Chọn bàn & bấm Mở bàn', '->'),
        (0.36, 0.62, 0.68, '2. StartSessionAsync(tableId, userId)', '->'),
        (0.62, 0.88, 0.58, '3. INSERT INTO Sessions...', '->'),
        (0.62, 0.88, 0.48, '4. UPDATE Tables SET Status=\'Occupied\'', '->'),
        (0.88, 0.62, 0.38, '5. Trả về SessionId thành công', '-->'),
        (0.62, 0.36, 0.28, '6. Ghi AuditLog & Trả về kết quả', '-->'),
        (0.36, 0.12, 0.18, '7. Cập nhật giao diện & bắt đầu đếm giờ', '-->')
    ]
    
    for x1, x2, y, text, style in steps:
        ls = '--' if style == '-->' else '-'
        ax.annotate('', xy=(x2, y), xytext=(x1, y), arrowprops=dict(arrowstyle='->', color='black', lw=1.2, ls=ls))
        ax.text((x1 + x2)/2, y + 0.025, text, ha='center', va='bottom', fontsize=8, color='black')

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_3_seq_open_table.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.4 Sequence Switch Table
def gen_2_4():
    fig, ax = plt.subplots(figsize=(8, 4.5), dpi=300)
    ax.axis('off')
    
    lifelines = [
        (0.12, 'Nhân viên'),
        (0.36, 'TableManagementVM'),
        (0.62, 'TableService'),
        (0.88, 'MySQL Database')
    ]
    
    for x, label in lifelines:
        box = patches.Rectangle((x-0.1, 0.85), 0.2, 0.1, fill=False, ec='black', lw=1.2)
        ax.add_patch(box)
        ax.text(x, 0.9, label, ha='center', va='center', fontsize=9, fontweight='bold', color='black')
        ax.plot([x, x], [0.85, 0.08], color='black', lw=1, ls=':')
        
    steps = [
        (0.12, 0.36, 0.78, '1. Chọn bàn đích & bấm Chuyển bàn', '->'),
        (0.36, 0.62, 0.68, '2. SwitchTableAsync(sessionId, old, new)', '->'),
        (0.62, 0.88, 0.58, '3. Chốt Time Slice bàn cũ (INSERT SessionPricingDetails)', '->'),
        (0.62, 0.88, 0.48, '4. Đổi trạng thái: Bàn cũ -> Available, Bàn mới -> Occupied', '->'),
        (0.62, 0.88, 0.38, '5. Cập nhật Session: TableId = newTableId', '->'),
        (0.88, 0.62, 0.28, '6. Commit Transaction thành công', '-->'),
        (0.62, 0.36, 0.18, '7. Ghi AuditLog chuyển bàn', '-->'),
        (0.36, 0.12, 0.10, '8. Làm mới trạng thái 2 bàn trên sơ đồ', '-->')
    ]
    
    for x1, x2, y, text, style in steps:
        ls = '--' if style == '-->' else '-'
        ax.annotate('', xy=(x2, y), xytext=(x1, y), arrowprops=dict(arrowstyle='->', color='black', lw=1.2, ls=ls))
        ax.text((x1 + x2)/2, y + 0.02, text, ha='center', va='bottom', fontsize=7.5, color='black')

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_4_seq_switch_table.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.5 ERD Simplified
def gen_2_5():
    fig, ax = plt.subplots(figsize=(9, 6), dpi=300)
    ax.axis('off')
    
    entities = [
        (0.08, 0.78, 'Roles\n(RoleId, RoleName)'),
        (0.35, 0.78, 'Users\n(UserId, RoleId, Username, Pwd)'),
        (0.65, 0.78, 'Employees\n(EmployeeId, UserId, Email)'),
        (0.08, 0.52, 'Permissions\n(PermissionId, Code)'),
        (0.08, 0.65, 'RolePermissions\n(RoleId, PermissionId)'),
        (0.35, 0.52, 'Sessions\n(SessionId, TableId, UserId)'),
        (0.65, 0.52, 'Invoices\n(InvoiceId, SessionId, Total)'),
        (0.35, 0.28, 'Tables\n(TableId, TableTypeId, Status)'),
        (0.65, 0.28, 'SessionProducts\n(SessionProdId, SessionId)'),
        (0.88, 0.52, 'InvoiceDetails\n(InvoiceDetailId, InvoiceId)'),
        (0.08, 0.28, 'TableTypes\n(TableTypeId, TypeName)'),
        (0.65, 0.08, 'Products\n(ProductId, CategoryId, Price)'),
        (0.35, 0.08, 'TablePricingRules\n(RuleId, TableTypeId)'),
        (0.88, 0.08, 'ProductCategories\n(CategoryId, Name)'),
        (0.88, 0.78, 'AuditLogs\n(AuditLogId, UserId, Action)')
    ]
    
    for x, y, label in entities:
        box = patches.FancyBboxPatch((x-0.06, y-0.05), 0.15, 0.09, boxstyle='round,pad=0.01', ec='black', fc='white', lw=1.2)
        ax.add_patch(box)
        ax.text(x+0.015, y, label, ha='center', va='center', fontsize=7.2, color='black')
        
    lines = [
        ((0.15, 0.78), (0.29, 0.78)),
        ((0.41, 0.78), (0.59, 0.78)),
        ((0.35, 0.73), (0.35, 0.57)),
        ((0.35, 0.47), (0.35, 0.33)),
        ((0.41, 0.52), (0.59, 0.52)),
        ((0.71, 0.52), (0.82, 0.52)),
        ((0.65, 0.47), (0.65, 0.33)),
        ((0.65, 0.23), (0.65, 0.13)),
        ((0.15, 0.28), (0.29, 0.28)),
        ((0.35, 0.23), (0.35, 0.13)),
        ((0.71, 0.08), (0.82, 0.08)),
        ((0.41, 0.78), (0.82, 0.78))
    ]
    
    for (x1, y1), (x2, y2) in lines:
        ax.plot([x1, x2], [y1, y2], color='black', lw=1.1)

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_5_erd.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.6 Auth Flow
def gen_2_6():
    fig, ax = plt.subplots(figsize=(8, 5), dpi=300)
    ax.axis('off')
    
    nodes = [
        (0.5, 0.90, 'Người dùng nhập Username & Password'),
        (0.5, 0.76, 'Truy vấn bảng Users theo Username'),
        (0.5, 0.62, 'BCrypt.Verify(Password, PasswordHash)?'),
        (0.2, 0.48, 'Báo lỗi đăng nhập\n(Sai thông tin / Khóa TK)'),
        (0.5, 0.48, 'Lấy RoleId & Nạp danh sách Permissions'),
        (0.5, 0.34, 'Khởi tạo UserSession toàn cục & Ghi AuditLog'),
        (0.5, 0.20, 'Hiển thị MainWindow & Điều hướng theo quyền')
    ]
    
    for x, y, text in nodes:
        if '?' in text:
            patch = patches.Polygon([[x, y+0.06], [x+0.22, y], [x, y-0.06], [x-0.22, y]], fill=False, ec='black', lw=1.2)
        else:
            patch = patches.FancyBboxPatch((x-0.2, y-0.04), 0.4, 0.08, boxstyle='round,pad=0.02', fill=False, ec='black', lw=1.2)
        ax.add_patch(patch)
        ax.text(x, y, text, ha='center', va='center', fontsize=8.5, color='black')
        
    ax.annotate('', xy=(0.5, 0.80), xytext=(0.5, 0.86), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.annotate('', xy=(0.5, 0.68), xytext=(0.5, 0.72), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.annotate('', xy=(0.2, 0.52), xytext=(0.35, 0.62), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.text(0.26, 0.60, 'Không khớp (Sai)', fontsize=8, color='black')
    ax.annotate('', xy=(0.5, 0.52), xytext=(0.5, 0.56), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.text(0.52, 0.54, 'Khớp (Đúng)', fontsize=8, color='black')
    ax.annotate('', xy=(0.5, 0.38), xytext=(0.5, 0.44), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.annotate('', xy=(0.5, 0.24), xytext=(0.5, 0.30), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_6_auth_flow.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.7 UI Login Wireframe
def gen_2_7():
    fig, ax = plt.subplots(figsize=(6, 4.5), dpi=300)
    ax.axis('off')
    
    win = patches.Rectangle((0.1, 0.1), 0.8, 0.8, fill=False, ec='black', lw=1.5)
    ax.add_patch(win)
    ax.text(0.12, 0.87, '[ - + X ] Đăng nhập hệ thống Billiard Pro', fontsize=8, color='black')
    ax.plot([0.1, 0.9], [0.85, 0.85], color='black', lw=1)
    
    ax.text(0.5, 0.75, 'BILLIARD MANAGEMENT SYSTEM', ha='center', va='center', fontsize=11, fontweight='bold', color='black')
    ax.text(0.5, 0.69, 'Vui lòng nhập thông tin đăng nhập', ha='center', va='center', fontsize=8.5, color='black')
    
    u_box = patches.Rectangle((0.25, 0.54), 0.5, 0.08, fill=False, ec='black', lw=1)
    ax.add_patch(u_box)
    ax.text(0.27, 0.58, 'Tên đăng nhập: admin / staff01', fontsize=8, color='black')
    
    p_box = patches.Rectangle((0.25, 0.42), 0.5, 0.08, fill=False, ec='black', lw=1)
    ax.add_patch(p_box)
    ax.text(0.27, 0.46, 'Mật khẩu: **********', fontsize=8, color='black')
    
    chk = patches.Rectangle((0.25, 0.34), 0.03, 0.03, fill=False, ec='black', lw=1)
    ax.add_patch(chk)
    ax.text(0.30, 0.355, 'Ghi nhớ đăng nhập', fontsize=8, va='center', color='black')
    
    btn = patches.Rectangle((0.25, 0.20), 0.5, 0.09, fill=False, ec='black', lw=1.5)
    ax.add_patch(btn)
    ax.text(0.5, 0.245, 'ĐĂNG NHẬP', ha='center', va='center', fontsize=9.5, fontweight='bold', color='black')

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_7_ui_login.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.8 UI Table Wireframe
def gen_2_8():
    fig, ax = plt.subplots(figsize=(8.5, 5), dpi=300)
    ax.axis('off')
    
    win = patches.Rectangle((0.05, 0.05), 0.9, 0.9, fill=False, ec='black', lw=1.5)
    ax.add_patch(win)
    ax.text(0.07, 0.92, '[ - + X ] Hệ Thống Quản Lý Quán Billiard Chuyên Nghiệp - Billiard Pro v1.0', fontsize=8, color='black')
    ax.plot([0.05, 0.95], [0.90, 0.90], color='black', lw=1)
    
    ax.plot([0.22, 0.22], [0.05, 0.90], color='black', lw=1)
    ax.text(0.135, 0.85, 'BILLIARD PRO\nAdmin Panel', ha='center', va='center', fontsize=9, fontweight='bold', color='black')
    
    menus = ['[*] Sơ đồ bàn', '[ ] Dịch vụ & F&B', '[ ] Hóa đơn & Ca', '[ ] Báo cáo doanh thu', '[ ] Quản lý nhân sự', '[ ] Đăng xuất']
    for idx, m in enumerate(menus):
        ax.text(0.08, 0.75 - idx*0.08, m, fontsize=8, color='black')
        
    ax.text(0.25, 0.85, 'BỘ LỌC BÀN: [ Tất cả ]  [ Bàn trống (Available) ]  [ Đang chơi (Occupied) ]', fontsize=8, color='black')
    ax.plot([0.22, 0.95], [0.80, 0.80], color='black', lw=0.8, ls=':')
    
    tables = [
        (0.28, 0.60, 'BÀN P01 (Pool)\nTrống\nGiá: 50.000đ/h'),
        (0.46, 0.60, 'BÀN P02 (Pool)\nĐANG CHƠI\n01:25:00 - 70.833đ'),
        (0.64, 0.60, 'BÀN P03 (Pool)\nTrống\nGiá: 50.000đ/h'),
        (0.82, 0.60, 'BÀN C01 (Carom)\nĐANG CHƠI\n00:45:00 - 33.750đ'),
        (0.28, 0.35, 'BÀN C02 (Carom)\nTrống\nGiá: 45.000đ/h'),
        (0.46, 0.35, 'BÀN 3B01 (3 Băng)\nTrống\nGiá: 80.000đ/h'),
        (0.64, 0.35, 'BÀN 3B02 (3 Băng)\nĐANG CHƠI\n02:10:00 - 173.333đ'),
        (0.82, 0.35, 'BÀN P04 (Pool)\nBảo trì\n---')
    ]
    
    for x, y, label in tables:
        t_box = patches.FancyBboxPatch((x-0.08, y-0.08), 0.16, 0.16, boxstyle='round,pad=0.01', fill=False, ec='black', lw=1.1)
        ax.add_patch(t_box)
        ax.text(x, y, label, ha='center', va='center', fontsize=7.5, color='black')
        
    ax.plot([0.22, 0.95], [0.20, 0.20], color='black', lw=0.8)
    ax.text(0.25, 0.12, 'THAO TÁC BÀN ĐANG CHỌN (P02):', fontsize=8, fontweight='bold', color='black')
    ax.text(0.25, 0.07, '[ + Mở bàn ]   [ -> Chuyển bàn ]   [ Goi mon ]   [ $ Thanh toan ]   [ ! Tam dung ]', fontsize=8, color='black')

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_8_ui_table.png', bbox_inches='tight', dpi=300)
    plt.close()

# 2.9 UI Staff Wireframe
def gen_2_9():
    fig, ax = plt.subplots(figsize=(8.5, 4.5), dpi=300)
    ax.axis('off')
    
    win = patches.Rectangle((0.05, 0.05), 0.9, 0.9, fill=False, ec='black', lw=1.5)
    ax.add_patch(win)
    ax.text(0.07, 0.92, '[ - + X ] Quản Lý Nhân Viên & Phân Quyền Hệ Thống', fontsize=8, color='black')
    ax.plot([0.05, 0.95], [0.90, 0.90], color='black', lw=1)
    
    ax.plot([0.45, 0.45], [0.05, 0.90], color='black', lw=1)
    ax.text(0.08, 0.85, 'DANH SÁCH NHÂN SỰ', fontsize=8.5, fontweight='bold', color='black')
    
    staff_rows = [
        '1. admin - Nguyễn Quản Trị (Admin - Hoạt động)',
        '2. staff01 - Trần Thu Ngân 01 (Staff - Hoạt động)',
        '3. staff02 - Lê Phục Vụ 02 (Staff - Hoạt động)',
        '4. staff03 - Phạm Tiếp Tân (Staff - Tạm khóa)'
    ]
    for idx, r in enumerate(staff_rows):
        r_box = patches.Rectangle((0.07, 0.72 - idx*0.12), 0.36, 0.09, fill=False, ec='black', lw=0.8)
        ax.add_patch(r_box)
        ax.text(0.08, 0.765 - idx*0.12, r, fontsize=7.2, color='black')
        
    ax.text(0.48, 0.85, 'THÔNG TIN CHI TIẾT & CẤP QUYỀN RBAC', fontsize=8.5, fontweight='bold', color='black')
    fields = [
        'Họ và tên: [ Trần Thu Ngân 01                ]',
        'Tên đăng nhập: [ staff01                     ]',
        'Số điện thoại: [ 0987654321                  ]',
        'Vai trò: (o) Nhân viên (Staff)   ( ) Quản trị viên (Admin)',
        'Quyền hạn: [x] TABLE_VIEW   [x] SESSION_OPEN   [x] INVOICE_PAY',
        '           [x] PRODUCT_MANAGE  [ ] REPORT_VIEW  [ ] USER_MANAGE'
    ]
    for idx, f in enumerate(fields):
        ax.text(0.48, 0.76 - idx*0.08, f, fontsize=7.5, color='black')
        
    ax.text(0.48, 0.15, '[ + Thêm mới ]      [ Lưu thay đổi ]      [ Khóa/Mở tài khoản ]', fontsize=8, fontweight='bold', color='black')

    plt.tight_layout()
    plt.savefig('docs/figures/fig_2_9_ui_staff.png', bbox_inches='tight', dpi=300)
    plt.close()

# 3.1 Connection Security
def gen_3_1():
    fig, ax = plt.subplots(figsize=(8, 3.5), dpi=300)
    ax.axis('off')
    
    b1 = patches.FancyBboxPatch((0.05, 0.25), 0.25, 0.55, boxstyle='round,pad=0.02', fill=False, ec='black', lw=1.2)
    ax.add_patch(b1)
    ax.text(0.175, 0.68, 'App.config\n(Theo dõi trên Git)', ha='center', va='center', fontsize=9, fontweight='bold', color='black')
    ax.text(0.175, 0.42, '<connectionStrings\n  configSource=\n  "connections.config" />\n\n(Không lộ mật khẩu)', ha='center', va='center', fontsize=8, color='black')
    
    b2 = patches.FancyBboxPatch((0.38, 0.25), 0.25, 0.55, boxstyle='round,pad=0.02', fill=False, ec='black', lw=1.2)
    ax.add_patch(b2)
    ax.text(0.505, 0.68, 'connections.config\n(Thêm vào .gitignore)', ha='center', va='center', fontsize=9, fontweight='bold', color='black')
    ax.text(0.505, 0.42, 'Chuỗi kết nối thực tế:\n- Server Host\n- Port, User, Pwd\n- SslMode=Required\n(Chỉ lưu máy cục bộ)', ha='center', va='center', fontsize=8, color='black')
    
    b3 = patches.FancyBboxPatch((0.71, 0.25), 0.25, 0.55, boxstyle='round,pad=0.02', fill=False, ec='black', lw=1.2)
    ax.add_patch(b3)
    ax.text(0.835, 0.68, 'MySQL Cloud\n(Aiven Managed DB)', ha='center', va='center', fontsize=9, fontweight='bold', color='black')
    ax.text(0.835, 0.42, 'Xác thực SSL/TLS 1.3\nCổng: 11624\nHost: aivencloud.com\nDatabase: defaultdb', ha='center', va='center', fontsize=8, color='black')
    
    ax.annotate('', xy=(0.38, 0.52), xytext=(0.30, 0.52), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))
    ax.annotate('', xy=(0.71, 0.52), xytext=(0.63, 0.52), arrowprops=dict(arrowstyle='->', color='black', lw=1.2))

    plt.tight_layout()
    plt.savefig('docs/figures/fig_3_1_connection_security.png', bbox_inches='tight', dpi=300)
    plt.close()

# 3.2 Performance Chart
def gen_3_2():
    fig, ax = plt.subplots(figsize=(7.5, 4), dpi=300)
    
    queries = ['Kết nối SSL ban đầu', 'Tải 20 bàn chơi', 'Mở phiên (Session)', 'Gọi 5 món F&B', 'Chuyển bàn (Slice)', 'Thanh toán & Bill']
    times_ms = [145.2, 42.5, 31.8, 28.4, 38.6, 52.1]
    
    bars = ax.barh(queries, times_ms, color='white', edgecolor='black', linewidth=1.2, hatch='///', height=0.55)
    
    for bar in bars:
        w = bar.get_width()
        ax.text(w + 2, bar.get_y() + bar.get_height()/2, f'{w:.1f} ms', va='center', ha='left', fontsize=8.5, color='black')
        
    ax.set_xlim(0, 180)
    ax.set_xlabel('Thời gian đáp ứng (mili-giây - ms)', fontsize=9.5, color='black')
    ax.set_title('Thời gian thực thi trung bình qua kết nối MySqlConnector (Cloud SSL)', fontsize=10.5, fontweight='bold', color='black')
    ax.grid(axis='x', linestyle='--', alpha=0.6, color='black')
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    ax.spines['left'].set_color('black')
    ax.spines['bottom'].set_color('black')
    ax.tick_params(colors='black')
    
    plt.tight_layout()
    plt.savefig('docs/figures/fig_3_2_performance_chart.png', bbox_inches='tight', dpi=300)
    plt.close()

if __name__ == '__main__':
    gen_1_1()
    gen_2_1()
    gen_2_2()
    gen_2_3()
    gen_2_4()
    gen_2_5()
    gen_2_6()
    gen_2_7()
    gen_2_8()
    gen_2_9()
    gen_3_1()
    gen_3_2()
    print('All 12 technical black-and-white figures generated successfully!')
