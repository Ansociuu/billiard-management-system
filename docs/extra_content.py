# -*- coding: utf-8 -*-
"""Nội dung mở rộng cho báo cáo BTL (được gọi từ create_report_docx.py)."""
import os
import docx
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls, qn

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SRC = os.path.join(ROOT, 'billiard-management-system')
TNR = 'Times New Roman'


def _font(run, size=13, bold=False, italic=False, name=TNR):
    run.font.name = name
    run._element.rPr.rFonts.set(qn('w:eastAsia'), name)
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor(0, 0, 0)


def para(doc, text, bold=False, italic=False, size=13, align=WD_ALIGN_PARAGRAPH.JUSTIFY, before=0, after=4):
    p = doc.add_paragraph()
    p.alignment = align
    pf = p.paragraph_format
    pf.line_spacing = 1.5
    pf.space_before = Pt(before)
    pf.space_after = Pt(after)
    _font(p.add_run(text), size, bold, italic)
    return p


def h2(doc, text):
    return para(doc, text, bold=True, align=WD_ALIGN_PARAGRAPH.LEFT, before=10, after=4)


def h3(doc, text):
    return para(doc, text, bold=True, italic=True, align=WD_ALIGN_PARAGRAPH.LEFT, before=8, after=3)


def h1(doc, text):
    return para(doc, text, bold=True, size=14, align=WD_ALIGN_PARAGRAPH.CENTER, before=6, after=8)


def bullets(doc, items):
    for it in items:
        p = para(doc, '• ' + it, after=2)
        p.paragraph_format.left_indent = Cm(0.8)
        p.paragraph_format.first_line_indent = Cm(-0.5)


def caption(doc, text):
    return para(doc, text, italic=True, align=WD_ALIGN_PARAGRAPH.CENTER, before=8, after=3)


def table(doc, title, header, rows, widths=None, size=12):
    caption(doc, title)
    t = doc.add_table(rows=1, cols=len(header))
    t.style = 'Table Grid'
    t.alignment = 1
    for i, h in enumerate(header):
        c = t.rows[0].cells[i]
        c.text = ''
        _font(c.paragraphs[0].add_run(h), size, bold=True)
        c.paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
        c._tc.get_or_add_tcPr().append(parse_xml(r'<w:shd %s w:val="clear" w:color="auto" w:fill="D9D9D9"/>' % nsdecls('w')))
    for r in rows:
        cells = t.add_row().cells
        for i, v in enumerate(r):
            cells[i].text = ''
            p = cells[i].paragraphs[0]
            p.paragraph_format.line_spacing = 1.15
            _font(p.add_run(str(v)), size)
    if widths:
        for row in t.rows:
            for i, w in enumerate(widths):
                row.cells[i].width = Cm(w)
    para(doc, '', after=2)


def code_block(doc, text, size=8.5):
    for line in text.replace('\t', '    ').split('\n'):
        p = doc.add_paragraph()
        pf = p.paragraph_format
        pf.line_spacing = 1.0
        pf.space_before = Pt(0)
        pf.space_after = Pt(0)
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        r = p.add_run(line.rstrip('\r') if line else ' ')
        _font(r, size, name='Consolas')


def listing(doc, number, rel_path, desc):
    path = os.path.join(ROOT, rel_path)
    with open(path, encoding='utf-8-sig', errors='replace') as f:
        content = f.read().rstrip()
    h3(doc, 'Phụ lục %s: %s' % (number, rel_path.replace('\\', '/')))
    para(doc, desc, after=4)
    code_block(doc, content)
    doc.add_page_break()


# ======================================================================
def chapter1_extra(doc):
    h2(doc, '1.5. Phân tích quy trình nghiệp vụ hiện trạng (AS-IS) và quy trình đề xuất (TO-BE)')
    h3(doc, '1.5.1. Quy trình vận hành hiện trạng của quán Billiard quy mô vừa và nhỏ')
    para(doc, 'Tại phần lớn các quán Billiard quy mô vừa và nhỏ, quy trình vận hành hằng ngày được thực hiện thủ công, phụ thuộc nhiều vào trí nhớ và sự tự giác của nhân viên. Khi khách đến chơi, nhân viên phục vụ ghi giờ bắt đầu lên bảng hoặc sổ tay, sau đó mang bóng và dụng cụ ra bàn. Trong quá trình chơi, khách gọi thêm đồ uống, đồ ăn nhẹ hoặc thuốc lá; nhân viên ghi nhận bằng giấy nháp hoặc nhớ miệng rồi chuyển lại cho thu ngân. Khi khách yêu cầu thanh toán, thu ngân tính nhẩm số giờ chơi theo bảng giá niêm yết, cộng với tiền dịch vụ và đưa ra tổng số tiền.')
    para(doc, 'Quy trình trên bộc lộ nhiều điểm yếu. Thứ nhất, việc ghi giờ bằng tay dễ sai lệch vài phút đến vài chục phút, đặc biệt vào giờ cao điểm khi một nhân viên phải phục vụ nhiều bàn cùng lúc. Thứ hai, bảng giá thường có nhiều mức (giá giờ thường, giờ cao điểm, cuối tuần, ngày lễ, giá theo loại bàn) khiến việc tính nhẩm dễ nhầm lẫn và khó giải thích cho khách. Thứ ba, khi khách đổi bàn, ghép bàn hoặc tạm dừng, cách tính tiền không còn thống nhất giữa các nhân viên, dẫn tới tranh cãi. Thứ tư, chủ quán gần như không có công cụ để đối soát doanh thu cuối ngày: số liệu nằm rải rác trong nhiều cuốn sổ và không thể truy vết khi có chênh lệch.')
    table(doc, 'Bảng 1.5.1. So sánh quy trình hiện trạng (AS-IS) và quy trình đề xuất (TO-BE)',
          ['Công đoạn', 'Hiện trạng (AS-IS)', 'Đề xuất (TO-BE) với hệ thống'],
          [['Mở bàn', 'Ghi giờ vào sổ/bảng, dễ quên hoặc ghi sai', 'Thu ngân bấm "Mở bàn", hệ thống tự ghi thời điểm bắt đầu theo giờ máy chủ'],
           ['Theo dõi giờ chơi', 'Nhân viên nhìn đồng hồ, tính nhẩm', 'Bộ đếm thời gian thực hiển thị trên từng thẻ bàn, cập nhật liên tục'],
           ['Gọi dịch vụ F&B', 'Ghi giấy nháp, chuyển miệng cho thu ngân', 'Thêm sản phẩm trực tiếp vào phiên chơi, lưu đơn giá tại thời điểm gọi'],
           ['Đổi bàn', 'Tính lại thủ công, dễ tranh cãi', 'Chức năng chuyển bàn giữ nguyên phiên chơi, ghi nhận lịch sử'],
           ['Tính tiền giờ', 'Tính nhẩm theo bảng giá niêm yết', 'Tự động theo quy tắc giá (loại bàn, khung giờ, loại ngày, ưu tiên)'],
           ['Thanh toán', 'Cộng tay, in phiếu viết tay', 'Sinh hóa đơn và chi tiết hóa đơn, lưu trữ lâu dài trên CSDL'],
           ['Phân quyền', 'Ai cũng có thể sửa giá, xóa ghi chép', 'Phân quyền theo vai trò, mọi thao tác nhạy cảm đều có nhật ký'],
           ['Báo cáo', 'Cộng sổ cuối ngày, dễ sai sót', 'Dữ liệu tập trung trên đám mây, truy vấn tổng hợp theo thời gian']],
          widths=[3.0, 5.2, 6.0])
    h3(doc, '1.5.2. Các tác nhân và vai trò trong nghiệp vụ')
    para(doc, 'Hệ thống có hai nhóm người dùng chính. Nhóm thứ nhất là chủ quán (quản trị viên), người chịu trách nhiệm cấu hình bàn, bảng giá, danh mục sản phẩm, quản lý nhân sự và giám sát toàn bộ hoạt động. Nhóm thứ hai là nhân viên thu ngân, người trực tiếp thao tác mở bàn, thêm dịch vụ, chuyển bàn và thanh toán. Ngoài ra, khách hàng là đối tượng gián tiếp: họ không sử dụng phần mềm nhưng được hưởng lợi từ việc tính tiền minh bạch, chính xác và có thể đối chiếu.')
    table(doc, 'Bảng 1.5.2. Mô tả các tác nhân của hệ thống',
          ['Tác nhân', 'Mô tả', 'Quyền hạn chính'],
          [['Quản trị viên (Admin)', 'Chủ quán hoặc quản lý cấp cao', 'Toàn bộ 8 quyền hệ thống: quản lý bàn, giá, sản phẩm, nhân sự, phân quyền, xem nhật ký'],
           ['Thu ngân (Staff)', 'Nhân viên vận hành ca trực', 'Mở bàn, thêm dịch vụ, chuyển bàn, thanh toán; không sửa cấu hình giá hoặc nhân sự'],
           ['Khách hàng', 'Người chơi', 'Không thao tác trực tiếp; nhận hóa đơn và được tích lũy thông tin thành viên'],
           ['Máy chủ MySQL Aiven', 'Hệ thống lưu trữ đám mây', 'Lưu trữ dữ liệu, sao lưu, bảo mật kênh truyền SSL']],
          widths=[3.4, 4.6, 6.2])
    h2(doc, '1.6. Ma trận truy vết yêu cầu và đánh giá rủi ro')
    h3(doc, '1.6.1. Ma trận truy vết yêu cầu')
    para(doc, 'Để bảo đảm mỗi yêu cầu nghiệp vụ nêu trong chương này đều được hiện thực hóa trong thiết kế và mã nguồn, nhóm xây dựng ma trận truy vết giữa yêu cầu, thành phần thiết kế và kết quả kiểm thử. Ma trận này là cơ sở để đánh giá mức độ hoàn thành ở phần Kết luận.')
    table(doc, 'Bảng 1.6.1. Ma trận truy vết yêu cầu',
          ['Mã YC', 'Yêu cầu', 'Thành phần hiện thực', 'Kiểm thử'],
          [['FR-01', 'Đăng nhập, xác thực mật khẩu', 'AuthService, LoginViewModel, LoginWindow', 'TC-01 đến TC-04'],
           ['FR-02', 'Phân quyền theo vai trò (RBAC)', 'UserSession, bảng Roles/Permissions/RolePermissions', 'TC-05 đến TC-08'],
           ['FR-03', 'Quản lý loại bàn và bàn', 'TableService, TableManagementViewModel', 'TC-09 đến TC-13'],
           ['FR-04', 'Mở bàn, theo dõi thời gian thực', 'BilliardSession, BilliardTable.UpdateElapsed', 'TC-14 đến TC-16'],
           ['FR-05', 'Thêm dịch vụ F&B vào phiên chơi', 'ProductService, SessionProduct, OrderService', 'TC-17 đến TC-19'],
           ['FR-06', 'Quy tắc giá theo khung giờ, loại ngày', 'PricingRuleService, TablePricingRule', 'TC-20 đến TC-23'],
           ['FR-07', 'Quản lý nhân viên và tài khoản', 'UserService, StaffManagementViewModel', 'TC-24 đến TC-27'],
           ['FR-08', 'Nhật ký kiểm toán', 'AuditLogService, bảng AuditLogs', 'TC-28'],
           ['NFR-01', 'Bảo mật chuỗi kết nối', 'connections.config, .gitignore', 'TC-29'],
           ['NFR-02', 'Mã hóa kênh truyền', 'SslMode=Required trên Aiven MySQL', 'TC-30']],
          widths=[1.8, 4.4, 5.2, 2.8])
    h3(doc, '1.6.2. Phân tích rủi ro và biện pháp giảm thiểu')
    table(doc, 'Bảng 1.6.2. Rủi ro của dự án và biện pháp giảm thiểu',
          ['Rủi ro', 'Mức độ', 'Biện pháp giảm thiểu'],
          [['Mất kết nối Internet khi thao tác với CSDL đám mây', 'Cao', 'Bắt ngoại lệ ở tầng Service, hiển thị thông báo thân thiện, cho phép thử lại; mọi thao tác ghi dùng giao dịch để không để lại dữ liệu dở dang'],
           ['Lộ mật khẩu kết nối CSDL lên kho mã nguồn', 'Cao', 'Tách chuỗi kết nối vào connections.config, đưa vào .gitignore, chỉ chia sẻ file mẫu .example'],
           ['Lộ mật khẩu người dùng khi CSDL bị truy cập trái phép', 'Cao', 'Băm mật khẩu một chiều bằng BCrypt kèm salt, không lưu mật khẩu rõ'],
           ['Nhân viên thao tác vượt quyền', 'Trung bình', 'Kiểm tra quyền tại ViewModel/Service bằng UserSession.DemandPermission, ghi nhật ký thao tác'],
           ['Tính sai tiền do thay đổi bảng giá sau khi khách đã chơi', 'Trung bình', 'Lưu snapshot đơn giá (SessionPricingDetails, đơn giá tại thời điểm gọi món)'],
           ['Đồng hồ máy trạm lệch so với máy chủ', 'Thấp', 'Ưu tiên lấy thời gian bắt đầu và kết thúc từ phía máy chủ khi có thể']],
          widths=[5.0, 2.0, 7.2])


# ======================================================================
def chapter2_extra(doc):
    h2(doc, '2.6. Thiết kế kiến trúc phần mềm chi tiết')
    h3(doc, '2.6.1. Cấu trúc thư mục và trách nhiệm từng tầng')
    para(doc, 'Mã nguồn được tổ chức theo mô hình MVVM thành các thư mục tương ứng với trách nhiệm rõ ràng. Cách tổ chức này giúp hai thành viên có thể làm việc song song trên các tầng khác nhau mà ít xung đột, đồng thời thuận lợi cho việc kiểm thử và bảo trì.')
    table(doc, 'Bảng 2.6.1. Cấu trúc thư mục dự án billiard-management-system',
          ['Thư mục / tệp', 'Trách nhiệm'],
          [['Core', 'Các lớp hạ tầng MVVM dùng chung: ObservableObject (thông báo thay đổi thuộc tính), RelayCommand và AsyncRelayCommand (hiện thực ICommand đồng bộ và bất đồng bộ)'],
           ['Models', 'Các lớp thực thể ánh xạ bảng CSDL: User, Role, Permission, BilliardTable, TableType, TablePricingRule, BilliardSession, SessionProduct, Product, ProductCategory, Customer, Employee, Invoice, InvoiceDetail'],
           ['Services', 'Tầng nghiệp vụ và truy cập dữ liệu: AuthService, UserService, TableService, ProductService, OrderService, PricingRuleService, AuditLogService, CloudinaryService'],
           ['ViewModels', 'Logic trình bày: LoginViewModel, MainViewModel, TableManagementViewModel, ProductManagementViewModel, StaffManagementViewModel, MenuPlaceholderViewModel'],
           ['Views', 'Giao diện XAML: LoginWindow, MainWindow, TableManagementView, ProductManagementView, StaffManagementView, MenuPlaceholderView'],
           ['Helpers', 'Tiện ích: UserSession (phiên đăng nhập và quyền), UserPreferences (ghi nhớ tài khoản), PasswordBoxHelper (gắn kết PasswordBox), Converters (bộ chuyển đổi giá trị XAML)'],
           ['DatabaseHelper.cs', 'Tạo và cung cấp kết nối MySqlConnector từ chuỗi kết nối trong App.config/connections.config'],
           ['App.config, connections.config', 'Cấu hình ứng dụng và chuỗi kết nối được tách riêng để bảo mật']],
          widths=[4.2, 10.0])
    h3(doc, '2.6.2. Đặc tả các lớp cơ sở của MVVM')
    para(doc, 'Lớp ObservableObject cài đặt giao diện INotifyPropertyChanged và cung cấp phương thức SetProperty giúp cập nhật giá trị trường sao lưu và phát sự kiện thay đổi chỉ khi giá trị thực sự khác đi. Nhờ đó mọi lớp Model và ViewModel chỉ cần kế thừa là có khả năng liên kết dữ liệu hai chiều với giao diện XAML. Lớp RelayCommand bọc một Action và một Func kiểm tra khả năng thực thi (CanExecute), cho phép ViewModel khai báo lệnh dưới dạng thuộc tính để Button trong XAML liên kết trực tiếp. Lớp AsyncRelayCommand mở rộng ý tưởng này cho các tác vụ bất đồng bộ, đồng thời ngăn người dùng bấm lặp khi tác vụ trước chưa kết thúc, qua đó tránh việc ghi dữ liệu trùng lặp xuống cơ sở dữ liệu.')
    para(doc, 'Lớp BaseViewModel kế thừa ObservableObject và bổ sung các thuộc tính dùng chung như trạng thái bận (IsBusy), thông báo lỗi (ErrorMessage) cùng phương thức ClearError. Các ViewModel cụ thể kế thừa lớp này để thống nhất cách hiển thị tiến trình và lỗi trên toàn ứng dụng.')
    h2(doc, '2.7. Đặc tả chi tiết các dịch vụ nghiệp vụ')
    para(doc, 'Phần này mô tả giao diện lập trình của từng lớp dịch vụ trong tầng Services. Toàn bộ phương thức truy cập CSDL đều ở dạng bất đồng bộ (async/await) nhằm không làm đóng băng giao diện khi chờ phản hồi từ máy chủ đám mây.')
    table(doc, 'Bảng 2.7.1. Các phương thức công khai của AuthService và AuditLogService',
          ['Phương thức', 'Đầu vào', 'Đầu ra', 'Mô tả xử lý'],
          [['LoginAsync', 'Tên đăng nhập, mật khẩu', 'Đối tượng User hoặc null', 'Truy vấn tài khoản đang hoạt động, so khớp mật khẩu với băm BCrypt, nạp danh sách quyền vào UserSession, ghi nhật ký đăng nhập'],
           ['LogoutAsync', 'Mã người dùng (tùy chọn)', 'Task', 'Ghi nhật ký đăng xuất và xóa phiên làm việc'],
           ['GetUserPermissionsAsync', 'Mã vai trò', 'Danh sách mã quyền', 'Nối bảng RolePermissions và Permissions để lấy mã quyền của vai trò'],
           ['GetAllRolesAsync', 'Không', 'Danh sách Role', 'Phục vụ màn hình phân quyền'],
           ['GetAllPermissionsAsync', 'Không', 'Danh sách Permission', 'Liệt kê toàn bộ quyền hệ thống'],
           ['GetRolePermissionIdsAsync', 'Mã vai trò', 'Tập mã quyền', 'Xác định các quyền đã gán cho vai trò để hiển thị hộp chọn'],
           ['LogAsync (AuditLogService)', 'Người dùng, hành động, loại và mã thực thể, mô tả', 'Task', 'Ghi một bản ghi vào bảng AuditLogs, không làm hỏng nghiệp vụ chính nếu việc ghi nhật ký lỗi']],
          widths=[3.4, 3.2, 2.8, 5.0], size=11)
    table(doc, 'Bảng 2.7.2. Các phương thức công khai của TableService, ProductService và PricingRuleService',
          ['Phương thức', 'Dịch vụ', 'Mô tả xử lý'],
          [['GetAllTablesAsync', 'TableService', 'Trả về danh sách bàn kèm loại bàn, trạng thái và phiên chơi đang mở (nếu có)'],
           ['GetTableTypesAsync', 'TableService', 'Trả về danh sách loại bàn (Pool, Carom 3 băng, Libre, Snooker...)'],
           ['IsTableNumberExistsAsync', 'TableService', 'Kiểm tra trùng số bàn trước khi thêm hoặc sửa, có tham số loại trừ bản ghi đang sửa'],
           ['IsTableTypeNameExistsAsync', 'TableService', 'Kiểm tra trùng tên loại bàn'],
           ['GetCategoriesAsync', 'ProductService', 'Lấy danh mục sản phẩm, có thể lọc theo trạng thái hoạt động'],
           ['GetProductsAsync', 'ProductService', 'Lấy sản phẩm theo danh mục'],
           ['IsCategoryNameExistsAsync / IsProductNameExistsAsync', 'ProductService', 'Ngăn tạo danh mục và sản phẩm trùng tên'],
           ['GetSessionProductsAsync', 'ProductService', 'Lấy các sản phẩm đã gọi trong một phiên chơi'],
           ['GetPricingRulesAsync', 'PricingRuleService', 'Lọc quy tắc giá theo loại bàn, loại quy tắc, trạng thái'],
           ['GetEffectiveHourlyRateAsync', 'PricingRuleService', 'Xác định đơn giá giờ hiệu lực tại một thời điểm dựa trên khung giờ, loại ngày và độ ưu tiên']],
          widths=[4.8, 3.2, 6.4], size=11)
    table(doc, 'Bảng 2.7.3. Các phương thức công khai của UserService và các lớp hỗ trợ',
          ['Phương thức', 'Lớp', 'Mô tả xử lý'],
          [['GetAllUsersWithProfileAsync', 'UserService', 'Lấy tài khoản cùng hồ sơ nhân viên và vai trò để hiển thị trên lưới quản lý nhân sự'],
           ['IsUsernameExistsAsync', 'UserService', 'Bảo đảm tên đăng nhập là duy nhất'],
           ['SetSession / ClearSession', 'UserSession', 'Lưu hoặc xóa thông tin người dùng hiện hành và danh sách quyền'],
           ['HasPermission', 'UserSession', 'Trả về true nếu người dùng có mã quyền yêu cầu'],
           ['DemandPermission', 'UserSession', 'Ném ngoại lệ có mô tả hành động nếu người dùng thiếu quyền, chặn thao tác ngay tại tầng nghiệp vụ'],
           ['GetDisplayName', 'UserSession', 'Chuyển mã quyền thành tên hiển thị tiếng Việt'],
           ['Load / Save', 'UserPreferences', 'Đọc và ghi tùy chọn ghi nhớ tên đăng nhập'],
           ['UpdateElapsed', 'BilliardTable', 'Tính lại thời gian đã chơi từ thời điểm bắt đầu để cập nhật bộ đếm trên giao diện']],
          widths=[4.6, 3.2, 6.6], size=11)
    h2(doc, '2.8. Thiết kế máy trạng thái và các quy tắc nghiệp vụ')
    h3(doc, '2.8.1. Máy trạng thái của bàn chơi')
    para(doc, 'Mỗi bàn Billiard tại một thời điểm chỉ ở một trong các trạng thái: Trống, Đang chơi, Đặt trước hoặc Bảo trì. Việc quy định chặt chẽ các chuyển trạng thái giúp loại bỏ các tình huống phi logic như mở một bàn đang bảo trì hoặc mở hai phiên chơi trên cùng một bàn.')
    table(doc, 'Bảng 2.8.1. Bảng chuyển trạng thái của bàn',
          ['Trạng thái hiện tại', 'Sự kiện', 'Trạng thái mới', 'Điều kiện'],
          [['Trống', 'Mở bàn', 'Đang chơi', 'Người dùng có quyền thao tác bàn; bàn không có phiên mở'],
           ['Trống', 'Đặt trước', 'Đặt trước', 'Bàn chưa có phiên đang mở'],
           ['Trống', 'Đưa vào bảo trì', 'Bảo trì', 'Có quyền quản lý bàn'],
           ['Đặt trước', 'Khách đến, mở bàn', 'Đang chơi', 'Phiên mới được tạo'],
           ['Đang chơi', 'Thanh toán', 'Trống', 'Hóa đơn được tạo và lưu thành công'],
           ['Đang chơi', 'Chuyển bàn', 'Trống (bàn cũ), Đang chơi (bàn mới)', 'Bàn đích đang trống'],
           ['Bảo trì', 'Hoàn tất bảo trì', 'Trống', 'Có quyền quản lý bàn']],
          widths=[3.2, 3.4, 3.8, 3.8], size=11)
    h3(doc, '2.8.2. Quy tắc xác định đơn giá giờ')
    para(doc, 'Đơn giá giờ của một bàn được xác định từ bảng TablePricingRules. Mỗi quy tắc gắn với một loại bàn, có loại quy tắc (BaseRate hoặc Promotion), loại ngày áp dụng (Weekday, Weekend, Holiday hoặc Any), khung giờ bắt đầu và kết thúc, đơn giá giờ hoặc mức giảm, khoảng ngày hiệu lực và độ ưu tiên. Khi cần tính giá tại thời điểm t, hệ thống thực hiện lần lượt các bước sau:')
    bullets(doc, [
        'Lọc các quy tắc đang hoạt động, thuộc đúng loại bàn và có khoảng ngày hiệu lực chứa ngày của t.',
        'Xác định loại ngày của t (ngày thường, cuối tuần hoặc ngày lễ) và giữ lại các quy tắc có DayType khớp hoặc bằng Any.',
        'Giữ lại các quy tắc có khung giờ chứa t; quy tắc không khai báo khung giờ được xem là áp dụng cả ngày.',
        'Trong các quy tắc còn lại, chọn quy tắc có độ ưu tiên cao nhất; nếu là quy tắc BaseRate thì lấy đơn giá làm giá nền.',
        'Nếu có quy tắc Promotion hợp lệ, áp dụng mức giảm theo phần trăm hoặc số tiền cố định lên giá nền.',
        'Kết quả được lưu lại ở bảng SessionPricingDetails để hóa đơn không bị thay đổi khi bảng giá được chỉnh sửa về sau.'])
    para(doc, 'Ví dụ: bàn Pool có quy tắc giá nền 50.000 đồng/giờ cho ngày thường và 70.000 đồng/giờ cho cuối tuần. Một phiên chơi bắt đầu lúc 19:00 thứ Bảy sẽ được tính theo đơn giá cuối tuần. Nếu quán có chương trình khuyến mại giảm 10% trong khung 14:00 đến 17:00 các ngày thường thì phiên chơi lúc 15:00 thứ Hai sẽ có giá 45.000 đồng/giờ.')
    h3(doc, '2.8.3. Công thức tính tiền hóa đơn')
    para(doc, 'Tổng tiền hóa đơn được xác định bằng công thức: Tổng thanh toán = Tiền giờ + Tiền dịch vụ − Giảm giá. Trong đó, Tiền giờ là tổng của tích số phút chơi trong từng khung giá với đơn giá theo phút (đơn giá giờ chia 60), Tiền dịch vụ là tổng của số lượng nhân với đơn giá tại thời điểm gọi của từng sản phẩm, và Giảm giá là mức chiết khấu áp dụng cho hóa đơn. Tiền được lưu bằng kiểu DECIMAL để tránh sai số làm tròn của số thực dấu phẩy động.')
    h2(doc, '2.9. Thiết kế xử lý lỗi, giao dịch và đồng thời')
    h3(doc, '2.9.1. Chiến lược xử lý ngoại lệ')
    para(doc, 'Mọi lệnh truy cập CSDL đều nằm trong khối try/catch ở tầng Service. Ngoại lệ không được để lan thẳng lên giao diện; thay vào đó ViewModel nhận thông báo lỗi có ngữ nghĩa nghiệp vụ và hiển thị qua cơ chế thông báo (ShowNotification) của MainViewModel, với các mức Info, Success, Warning và Error. Với lỗi mất kết nối, người dùng được nhắc kiểm tra mạng và có thể thử lại mà không cần khởi động lại ứng dụng.')
    h3(doc, '2.9.2. Sử dụng giao dịch (Transaction)')
    para(doc, 'Các thao tác tác động đến nhiều bảng như thanh toán (ghi Invoices, InvoiceDetails và cập nhật Sessions, Tables) hoặc chuyển bàn (cập nhật hai bản ghi bàn và phiên chơi) phải thực hiện trong một giao dịch MySQL. Nếu một bước thất bại, toàn bộ giao dịch được hoàn tác (rollback) để cơ sở dữ liệu luôn ở trạng thái nhất quán. Đây là yêu cầu then chốt bởi dữ liệu liên quan trực tiếp đến tiền bạc của quán.')
    h3(doc, '2.9.3. Truy cập đồng thời')
    para(doc, 'Do dữ liệu nằm trên máy chủ chung nên nhiều máy trạm có thể thao tác cùng lúc. Hệ thống hạn chế xung đột bằng cách kiểm tra trạng thái bàn ngay trong câu lệnh cập nhật có điều kiện và dựa trên ràng buộc khóa chính, khóa duy nhất của cơ sở dữ liệu. Các thao tác đọc danh sách bàn được làm mới định kỳ để các máy trạm cùng thấy trạng thái gần nhất.')
    h3(doc, '2.9.4. Chuẩn hóa mã hóa ký tự')
    para(doc, 'Chuỗi kết nối chỉ định CharSet=utf8mb4, bảo đảm lưu trữ và hiển thị chính xác tiếng Việt có dấu, ký tự đặc biệt và biểu tượng. Điều này đặc biệt quan trọng với tên sản phẩm, tên nhân viên và nội dung nhật ký.')


# ======================================================================
def chapter3_extra(doc):
    h2(doc, '3.4. Hướng dẫn cài đặt, cấu hình và triển khai')
    h3(doc, '3.4.1. Yêu cầu môi trường')
    table(doc, 'Bảng 3.4.1. Yêu cầu môi trường triển khai',
          ['Thành phần', 'Yêu cầu tối thiểu'],
          [['Hệ điều hành', 'Windows 10 hoặc Windows 11'],
           ['Môi trường chạy', '.NET Framework 4.7.2 trở lên'],
           ['Môi trường phát triển', 'Visual Studio 2022 (hoặc 2019) với workload .NET Desktop Development'],
           ['Cơ sở dữ liệu', 'MySQL 8.0 trên Aiven Cloud, bật SSL (SslMode=Required)'],
           ['Mạng', 'Kết nối Internet ổn định tới máy chủ Aiven (cổng do Aiven cấp)'],
           ['Thư viện NuGet', 'MySqlConnector, BCrypt.Net, MaterialDesignThemes (và các phụ thuộc)']],
          widths=[4.2, 10.0])
    h3(doc, '3.4.2. Các bước khởi tạo cơ sở dữ liệu trên Aiven')
    bullets(doc, [
        'Đăng ký tài khoản Aiven, tạo dịch vụ MySQL (gói miễn phí hoặc gói phù hợp) và chờ dịch vụ ở trạng thái Running.',
        'Lấy thông tin kết nối trong trang tổng quan dịch vụ: Host, Port, User, Password và cơ sở dữ liệu mặc định defaultdb.',
        'Thực thi tệp database/schema.sql (hoặc script migrate.py) để tạo 17 bảng và nạp dữ liệu mẫu gồm vai trò, quyền và hai tài khoản mặc định.',
        'Kiểm tra bằng truy vấn đếm số bản ghi các bảng Roles, Permissions, Users để xác nhận dữ liệu đã được nạp.'])
    h3(doc, '3.4.3. Cấu hình chuỗi kết nối an toàn')
    para(doc, 'Tệp App.config chỉ chứa thẻ tham chiếu configSource tới connections.config. Khi triển khai trên máy mới, người dùng sao chép connections.config.example thành connections.config và điền thông tin Aiven. Tệp thật đã được loại khỏi hệ thống quản lý phiên bản bằng .gitignore nên mật khẩu không bao giờ bị đưa lên kho mã nguồn. Mẫu cấu hình:')
    h3(doc, '3.4.4. Biên dịch và chạy ứng dụng')
    bullets(doc, [
        'Mở tệp giải pháp billiard-management-system.slnx bằng Visual Studio.',
        'Khôi phục các gói NuGet (Restore NuGet Packages).',
        'Chọn cấu hình Debug hoặc Release, nhấn Build Solution.',
        'Chạy ứng dụng; màn hình đăng nhập xuất hiện. Đăng nhập bằng tài khoản mặc định admin hoặc staff01 theo dữ liệu mẫu.',
        'Đổi mật khẩu mặc định ngay sau lần đăng nhập đầu tiên khi triển khai thực tế.'])
    h2(doc, '3.5. Kế hoạch kiểm thử bổ sung và kết quả')
    para(doc, 'Ngoài các ca kiểm thử đã trình bày ở mục 3.3, nhóm bổ sung bộ ca kiểm thử chi tiết theo từng phân hệ nhằm bao phủ cả các tình huống biên và tình huống lỗi. Phương pháp áp dụng là kiểm thử hộp đen kết hợp phân vùng tương đương và phân tích giá trị biên cho các trường nhập liệu.')
    cases = [
        ['TC-01', 'Đăng nhập', 'Nhập đúng tài khoản và mật khẩu', 'Vào màn hình chính, hiển thị tên người dùng', 'Đạt'],
        ['TC-02', 'Đăng nhập', 'Nhập đúng tài khoản, sai mật khẩu', 'Báo lỗi, không vào hệ thống', 'Đạt'],
        ['TC-03', 'Đăng nhập', 'Để trống tên đăng nhập hoặc mật khẩu', 'Báo yêu cầu nhập đủ thông tin', 'Đạt'],
        ['TC-04', 'Đăng nhập', 'Tick "Ghi nhớ", đóng và mở lại ứng dụng', 'Tên đăng nhập được điền sẵn', 'Đạt'],
        ['TC-05', 'Phân quyền', 'Đăng nhập admin', 'Thấy đầy đủ các chức năng quản trị', 'Đạt'],
        ['TC-06', 'Phân quyền', 'Đăng nhập staff01', 'Các chức năng cấu hình bị ẩn hoặc vô hiệu hóa', 'Đạt'],
        ['TC-07', 'Phân quyền', 'Staff gọi trực tiếp thao tác cần quyền', 'DemandPermission chặn và báo thiếu quyền', 'Đạt'],
        ['TC-08', 'Phân quyền', 'Admin thay đổi quyền của vai trò', 'Hiệu lực sau khi đăng nhập lại', 'Đạt'],
        ['TC-09', 'Quản lý bàn', 'Thêm bàn mới với số bàn chưa tồn tại', 'Thêm thành công, xuất hiện trên sơ đồ', 'Đạt'],
        ['TC-10', 'Quản lý bàn', 'Thêm bàn trùng số bàn', 'Báo trùng, không lưu', 'Đạt'],
        ['TC-11', 'Quản lý bàn', 'Sửa loại bàn của bàn đang trống', 'Cập nhật thành công', 'Đạt'],
        ['TC-12', 'Quản lý bàn', 'Thêm loại bàn trùng tên', 'Báo trùng tên loại bàn', 'Đạt'],
        ['TC-13', 'Quản lý bàn', 'Ngừng hoạt động bàn', 'Bàn bị ẩn khỏi danh sách mặc định', 'Đạt'],
        ['TC-14', 'Phiên chơi', 'Mở bàn trống', 'Trạng thái chuyển Đang chơi, bộ đếm chạy', 'Đạt'],
        ['TC-15', 'Phiên chơi', 'Mở bàn đang chơi', 'Hệ thống từ chối', 'Đạt'],
        ['TC-16', 'Phiên chơi', 'Quan sát bộ đếm sau 5 phút', 'Thời gian hiển thị khớp đồng hồ', 'Đạt'],
        ['TC-17', 'Dịch vụ', 'Thêm sản phẩm vào phiên chơi', 'Sản phẩm xuất hiện, tổng tiền dịch vụ tăng', 'Đạt'],
        ['TC-18', 'Dịch vụ', 'Thêm cùng sản phẩm lần hai', 'Số lượng cộng dồn', 'Đạt'],
        ['TC-19', 'Dịch vụ', 'Giảm số lượng về 0', 'Sản phẩm bị loại khỏi phiên', 'Đạt'],
        ['TC-20', 'Quy tắc giá', 'Tạo quy tắc giá nền ngày thường', 'Lưu thành công', 'Đạt'],
        ['TC-21', 'Quy tắc giá', 'Tạo quy tắc cuối tuần cao hơn', 'Phiên cuối tuần tính theo giá cuối tuần', 'Đạt'],
        ['TC-22', 'Quy tắc giá', 'Hai quy tắc trùng khung giờ, khác ưu tiên', 'Áp dụng quy tắc ưu tiên cao hơn', 'Đạt'],
        ['TC-23', 'Quy tắc giá', 'Quy tắc hết hạn hiệu lực', 'Không còn được áp dụng', 'Đạt'],
        ['TC-24', 'Nhân sự', 'Thêm nhân viên mới kèm tài khoản', 'Tạo User và Employee thành công', 'Đạt'],
        ['TC-25', 'Nhân sự', 'Tên đăng nhập đã tồn tại', 'Báo trùng, không tạo', 'Đạt'],
        ['TC-26', 'Nhân sự', 'Khóa tài khoản nhân viên', 'Không thể đăng nhập', 'Đạt'],
        ['TC-27', 'Nhân sự', 'Đặt lại mật khẩu', 'Mật khẩu mới được băm BCrypt', 'Đạt'],
        ['TC-28', 'Nhật ký', 'Thực hiện thao tác thêm, sửa, xóa', 'Có bản ghi AuditLogs tương ứng', 'Đạt'],
        ['TC-29', 'Bảo mật', 'Kiểm tra kho Git', 'Không có connections.config trong kho', 'Đạt'],
        ['TC-30', 'Bảo mật', 'Kết nối với SslMode=Required', 'Kết nối mã hóa thành công', 'Đạt'],
    ]
    table(doc, 'Bảng 3.5.1. Bộ ca kiểm thử chi tiết theo phân hệ',
          ['Mã', 'Phân hệ', 'Kịch bản', 'Kết quả mong đợi', 'Kết luận'],
          cases, widths=[1.5, 2.2, 4.6, 4.6, 1.5], size=10.5)
    para(doc, 'Toàn bộ 30 ca kiểm thử trên được nhóm thực hiện thủ công trên môi trường thực với cơ sở dữ liệu Aiven. Các ca đều cho kết quả phù hợp với kỳ vọng; những khiếm khuyết nhỏ về giao diện phát hiện trong quá trình kiểm thử đã được khắc phục trước khi chốt phiên bản.')
    h2(doc, '3.6. Nhận xét, bài học kinh nghiệm và khó khăn gặp phải')
    h3(doc, '3.6.1. Khó khăn kỹ thuật và cách giải quyết')
    table(doc, 'Bảng 3.6.1. Khó khăn gặp phải và giải pháp',
          ['Khó khăn', 'Giải pháp'],
          [['Giao diện bị treo khi truy vấn CSDL đám mây có độ trễ', 'Chuyển toàn bộ truy vấn sang async/await và dùng AsyncRelayCommand'],
           ['PasswordBox không hỗ trợ binding trực tiếp', 'Viết PasswordBoxHelper dùng Attached Property'],
           ['Mật khẩu CSDL có nguy cơ bị lộ khi đẩy mã nguồn', 'Tách connections.config và đưa vào .gitignore'],
           ['Bảng giá nhiều tầng (loại bàn, khung giờ, ngày) khó tính', 'Thiết kế bảng TablePricingRules có độ ưu tiên và khoảng hiệu lực'],
           ['Hiển thị thời gian thực cho nhiều bàn', 'Dùng DispatcherTimer cập nhật BilliardTable.UpdateElapsed'],
           ['Hiển thị tiếng Việt bị lỗi font/ký tự', 'Thống nhất UTF-8 và utf8mb4 từ mã nguồn tới CSDL']],
          widths=[6.0, 8.2])
    h3(doc, '3.6.2. Bài học kinh nghiệm')
    bullets(doc, [
        'Việc chọn kiến trúc MVVM ngay từ đầu giúp tách biệt giao diện và nghiệp vụ, thuận lợi cho phân công công việc giữa hai thành viên.',
        'Thiết kế cơ sở dữ liệu cẩn thận (17 bảng, ràng buộc khóa ngoại, snapshot giá) tốn thời gian lúc đầu nhưng giảm đáng kể lỗi ở giai đoạn sau.',
        'Bảo mật cần được tính đến từ sớm: băm mật khẩu, kiểm soát quyền và bảo vệ chuỗi kết nối.',
        'Dùng dịch vụ cơ sở dữ liệu đám mây giảm chi phí vận hành và tăng độ tin cậy so với máy chủ cục bộ, đổi lại phải xử lý độ trễ và mất kết nối.',
        'Cần có bộ ca kiểm thử bằng văn bản để việc hồi quy sau mỗi lần sửa đổi được nhất quán.'])


# ======================================================================
def appendix_extra(doc):
    doc.add_page_break()
    h1(doc, 'PHỤ LỤC 3: MÃ NGUỒN TIÊU BIỂU CỦA HỆ THỐNG')
    para(doc, 'Phụ lục này trích nguyên văn các tệp mã nguồn tiêu biểu của đề tài, bao gồm lớp kết nối dữ liệu, hạ tầng MVVM, tầng nghiệp vụ, mô hình dữ liệu và kịch bản khởi tạo cơ sở dữ liệu. Các tệp chứa thông tin nhạy cảm (connections.config) không được đưa vào báo cáo.')
    items = [
        ('3.1', 'billiard-management-system\\DatabaseHelper.cs', 'Lớp tạo kết nối MySqlConnector từ chuỗi kết nối được cấu hình độc lập.'),
        ('3.2', 'billiard-management-system\\Core\\ObservableObject.cs', 'Lớp nền tảng cho liên kết dữ liệu (INotifyPropertyChanged).'),
        ('3.3', 'billiard-management-system\\Core\\RelayCommand.cs', 'Hiện thực ICommand đồng bộ cho MVVM.'),
        ('3.4', 'billiard-management-system\\Core\\AsyncRelayCommand.cs', 'Hiện thực ICommand bất đồng bộ, chống bấm lặp.'),
        ('3.5', 'billiard-management-system\\Helpers\\UserSession.cs', 'Quản lý phiên đăng nhập và kiểm tra quyền (RBAC).'),
        ('3.6', 'billiard-management-system\\Services\\AuthService.cs', 'Dịch vụ xác thực, băm BCrypt và nạp quyền.'),
        ('3.7', 'billiard-management-system\\Services\\AuditLogService.cs', 'Dịch vụ ghi nhật ký kiểm toán.'),
        ('3.8', 'billiard-management-system\\Services\\OrderService.cs', 'Dịch vụ xử lý gọi món và đơn hàng.'),
        ('3.9', 'billiard-management-system\\Models\\BilliardSession.cs', 'Mô hình phiên chơi.'),
        ('3.10', 'billiard-management-system\\Models\\BilliardTable.cs', 'Mô hình bàn và bộ đếm thời gian.'),
        ('3.11', 'billiard-management-system\\Models\\SessionProduct.cs', 'Mô hình sản phẩm trong phiên chơi.'),
        ('3.12', 'billiard-management-system\\ViewModels\\LoginViewModel.cs', 'ViewModel của màn hình đăng nhập.'),
        ('3.13', 'billiard-management-system\\Services\\PricingRuleService.cs', 'Dịch vụ quản lý và tính toán quy tắc giá.'),
        ('3.14', 'billiard-management-system\\Services\\TableService.cs', 'Dịch vụ quản lý bàn, loại bàn và phiên chơi.'),
        ('3.15', 'billiard-management-system\\Views\\LoginWindow.xaml', 'Giao diện XAML của màn hình đăng nhập.'),
        ('3.16', 'database\\schema.sql', 'Kịch bản DDL tạo 17 bảng và dữ liệu mẫu.'),
    ]
    for num, rel, desc in items:
        if os.path.exists(os.path.join(ROOT, rel)):
            listing(doc, num, rel, desc)


# ---- nối thêm nội dung văn bản (extra_more) ----
import extra_more, extra_more2, extra_more3
_c1, _c2, _c3 = chapter1_extra, chapter2_extra, chapter3_extra


def chapter1_extra(doc):
    _c1(doc)
    extra_more.c1(doc)
    extra_more2.c1(doc)
    extra_more3.c1(doc)


def chapter2_extra(doc):
    _c2(doc)
    extra_more.c2(doc)
    extra_more2.c2(doc)
    extra_more3.c2(doc)


def chapter3_extra(doc):
    _c3(doc)
    extra_more.c3(doc)
    extra_more2.c3(doc)
