import docx
from docx.shared import Inches, Pt, RGBColor, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn
import os

def create_report_docx():
    doc = docx.Document()
    
    # 1. Page Setup: A4, Margins: Top 2.5, Bottom 2.5, Left 3.5, Right 2.5 cm
    for section in doc.sections:
        section.page_width = Cm(21.0)
        section.page_height = Cm(29.7)
        section.top_margin = Cm(2.5)
        section.bottom_margin = Cm(2.5)
        section.left_margin = Cm(3.5)
        section.right_margin = Cm(2.5)
        section.different_first_page_header_footer = False
        
        # Add Page Number in Header Center
        header = section.header
        p_head = header.paragraphs[0]
        p_head.alignment = WD_ALIGN_PARAGRAPH.CENTER
        # Add page field to header
        run_head = p_head.add_run()
        fldSimple = OxmlElement('w:fldSimple')
        fldSimple.set(qn('w:instr'), 'PAGE')
        run_head._r.append(fldSimple)
        run_head.font.name = 'Times New Roman'
        run_head.font.size = Pt(10)
        run_head.font.color.rgb = RGBColor(0, 0, 0)
        
    # Set default style
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Times New Roman'
    normal_style.font.size = Pt(13)
    normal_style.font.color.rgb = RGBColor(0, 0, 0)
    normal_style.paragraph_format.line_spacing = 1.5
    normal_style.paragraph_format.space_after = Pt(4)
    normal_style.paragraph_format.space_before = Pt(0)
    
    # Helper functions
    def set_cell_border(cell, **kwargs):
        """
        Set cell borders (black only)
        kwargs: top, bottom, left, right
        values: dict(sz=4, val='single', color='000000')
        """
        tcPr = cell._tc.get_or_add_tcPr()
        tcBorders = parse_xml(r'<w:tcBorders %s/>' % nsdecls('w'))
        for border_name in ['top', 'left', 'bottom', 'right']:
            if border_name in kwargs:
                edge = parse_xml(r'<w:%s %s w:val="%s" w:sz="%s" w:space="0" w:color="%s"/>' % (
                    border_name, nsdecls('w'), kwargs[border_name].get('val', 'single'),
                    kwargs[border_name].get('sz', '4'), kwargs[border_name].get('color', '000000')
                ))
                tcBorders.append(edge)
            else:
                edge = parse_xml(r'<w:%s %s w:val="none"/>' % (border_name, nsdecls('w')))
                tcBorders.append(edge)
        tcPr.append(tcBorders)

    def add_para(text, align=WD_ALIGN_PARAGRAPH.JUSTIFY, bold=False, italic=False, size=13, space_before=0, space_after=4):
        p = doc.add_paragraph()
        p.alignment = align
        p.paragraph_format.line_spacing = 1.5
        p.paragraph_format.space_before = Pt(space_before)
        p.paragraph_format.space_after = Pt(space_after)
        run = p.add_run(text)
        run.bold = bold
        run.italic = italic
        run.font.name = 'Times New Roman'
        run.font.size = Pt(size)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_heading_1(text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_before = Pt(14)
        p.paragraph_format.space_after = Pt(6)
        p.paragraph_format.line_spacing = 1.5
        run = p.add_run(text)
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(14)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_heading_2(text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_before = Pt(10)
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.line_spacing = 1.5
        run = p.add_run(text)
        run.bold = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(13)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_heading_3(text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_before = Pt(8)
        p.paragraph_format.space_after = Pt(3)
        p.paragraph_format.line_spacing = 1.5
        run = p.add_run(text)
        run.bold = True
        run.italic = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(13)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_heading_4(text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_before = Pt(6)
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.5
        run = p.add_run(text)
        run.italic = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(13)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_table_title(title_text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_before = Pt(8)
        p.paragraph_format.space_after = Pt(3)
        run = p.add_run(title_text)
        run.italic = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(13)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_fig_title(fig_text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_before = Pt(3)
        p.paragraph_format.space_after = Pt(10)
        run = p.add_run(fig_text)
        run.italic = True
        run.font.name = 'Times New Roman'
        run.font.size = Pt(13)
        run.font.color.rgb = RGBColor(0, 0, 0)
        return p

    def add_image_safe(img_path, width=Cm(14.5)):
        if os.path.exists(img_path):
            p = doc.add_paragraph()
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.paragraph_format.space_before = Pt(6)
            p.paragraph_format.space_after = Pt(2)
            run = p.add_run()
            run.add_picture(img_path, width=width)

    # ==========================
    # 1. TRANG BÌA CHÍNH (Mẫu BTL-01)
    # ==========================
    add_para('ĐẠI HỌC PHENIKAA', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=14, space_after=2)
    add_para('KHOA CÔNG NGHỆ THÔNG TIN', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=13, space_after=20)
    
    add_para('---------------------------------------', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=12, space_after=30)
    
    add_para('BÀI TẬP LỚN', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=22, space_after=14)
    add_para('HỌC PHẦN: CÔNG NGHỆ .NET', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=16, space_after=24)
    
    add_para('ĐỀ TÀI:', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=14, space_after=6)
    add_para('XÂY DỰNG HỆ THỐNG QUẢN LÝ QUÁN BILLIARD (BILLIARD PRO) TRÊN NỀN TẢNG C# .NET WPF VÀ CƠ SỞ DỮ LIỆU MYSQL', 
             align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=16, space_after=40)
             
    # Table info on main cover
    t_cover = doc.add_table(rows=2, cols=2)
    t_cover.alignment = WD_TABLE_ALIGNMENT.CENTER
    t_cover.rows[0].cells[0].paragraphs[0].text = "Sinh viên thực hiện:"
    t_cover.rows[0].cells[0].paragraphs[0].runs[0].font.bold = True
    t_cover.rows[0].cells[1].paragraphs[0].text = "Nguyễn Văn An, Lê Phạm Thành Đạt"
    
    t_cover.rows[1].cells[0].paragraphs[0].text = "Giảng viên hướng dẫn:"
    t_cover.rows[1].cells[0].paragraphs[0].runs[0].font.bold = True
    t_cover.rows[1].cells[1].paragraphs[0].text = "TS. Phạm Ngọc Hưng"
    
    for r in t_cover.rows:
        for c in r.cells:
            for p in c.paragraphs:
                p.paragraph_format.line_spacing = 1.5
                for run in p.runs:
                    run.font.name = 'Times New Roman'
                    run.font.size = Pt(13)
                    run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c)
            
    doc.add_paragraph().paragraph_format.space_before = Pt(60)
    add_para('NGÀNH: CÔNG NGHỆ THÔNG TIN', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=13, space_after=6)
    add_para('Hà Nội – Năm 2026', align=WD_ALIGN_PARAGRAPH.CENTER, bold=False, size=13, space_after=0)
    
    doc.add_page_break()

    # ==========================
    # 2. TRANG BÌA PHỤ (Mẫu BTL-02)
    # ==========================
    add_para('ĐẠI HỌC PHENIKAA', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=14, space_after=2)
    add_para('KHOA CÔNG NGHỆ THÔNG TIN', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=13, space_after=20)
    
    add_para('---------------------------------------', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=12, space_after=26)
    
    add_para('BÀI TẬP LỚN', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=22, space_after=14)
    add_para('HỌC PHẦN: CÔNG NGHỆ .NET', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=16, space_after=20)
    
    add_para('ĐỀ TÀI:', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=14, space_after=6)
    add_para('XÂY DỰNG HỆ THỐNG QUẢN LÝ QUÁN BILLIARD (BILLIARD PRO) TRÊN NỀN TẢNG C# .NET WPF VÀ CƠ SỞ DỮ LIỆU MYSQL', 
             align=WD_ALIGN_PARAGRAPH.CENTER, bold=True, size=16, space_after=30)

    # Sub cover table
    t_sub = doc.add_table(rows=3, cols=3)
    t_sub.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell_head = t_sub.rows[0].cells
    cell_head[0].paragraphs[0].text = "STT"
    cell_head[0].paragraphs[0].runs[0].font.bold = True
    cell_head[1].paragraphs[0].text = "Họ và tên sinh viên"
    cell_head[1].paragraphs[0].runs[0].font.bold = True
    cell_head[2].paragraphs[0].text = "Lớp"
    cell_head[2].paragraphs[0].runs[0].font.bold = True
    
    r1 = t_sub.rows[1].cells
    r1[0].paragraphs[0].text = "1"
    r1[1].paragraphs[0].text = "Nguyễn Văn An"
    r1[2].paragraphs[0].text = "K15-CNTT"
    
    r2 = t_sub.rows[2].cells
    r2[0].paragraphs[0].text = "2"
    r2[1].paragraphs[0].text = "Lê Phạm Thành Đạt"
    r2[2].paragraphs[0].text = "K15-CNTT"
    
    border_spec = dict(val='single', sz='4', color='000000')
    for r in t_sub.rows:
        for c in r.cells:
            for p in c.paragraphs:
                p.paragraph_format.line_spacing = 1.5
                for run in p.runs:
                    run.font.name = 'Times New Roman'
                    run.font.size = Pt(13)
                    run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)
            
    doc.add_paragraph().paragraph_format.space_before = Pt(20)
    add_para('Giảng viên hướng dẫn: TS. Phạm Ngọc Hưng', align=WD_ALIGN_PARAGRAPH.LEFT, bold=True, size=13, space_after=40)
    add_para('Hà Nội – Năm 2026', align=WD_ALIGN_PARAGRAPH.CENTER, bold=False, size=13, space_after=0)
    
    doc.add_page_break()

    # ==========================
    # 3. DANH SÁCH SINH VIÊN & PHÂN CÔNG NHIỆM VỤ
    # ==========================
    add_heading_1('DANH SÁCH SINH VIÊN THỰC HIỆN VÀ PHÂN CÔNG NHIỆM VỤ')
    
    add_table_title('Bảng 0.1: Phân công nhiệm vụ thực hiện đề tài')
    t_assign = doc.add_table(rows=3, cols=4)
    t_assign.alignment = WD_TABLE_ALIGNMENT.CENTER
    headers = ['STT', 'Họ và tên', 'Nhiệm vụ đảm nhiệm', 'Mức độ hoàn thành']
    for idx, h in enumerate(headers):
        t_assign.rows[0].cells[idx].paragraphs[0].text = h
        t_assign.rows[0].cells[idx].paragraphs[0].runs[0].font.bold = True
        
    t_assign.rows[1].cells[0].paragraphs[0].text = '1'
    t_assign.rows[1].cells[1].paragraphs[0].text = 'Nguyễn Văn An'
    t_assign.rows[1].cells[2].paragraphs[0].text = '- Khảo sát yêu cầu, thiết kế kiến trúc MVVM và CSDL (17 bảng).\n- Xây dựng tầng Service (TableService, OrderService, UserService).\n- Lập trình giao diện Sơ đồ bàn, Mở bàn, Đổi bàn và Thanh toán.'
    t_assign.rows[1].cells[3].paragraphs[0].text = '100%'

    t_assign.rows[2].cells[0].paragraphs[0].text = '2'
    t_assign.rows[2].cells[1].paragraphs[0].text = 'Lê Phạm Thành Đạt'
    t_assign.rows[2].cells[2].paragraphs[0].text = '- Xây dựng cơ chế xác thực BCrypt, phân quyền RBAC và UserSession.\n- Quản lý nhật ký kiểm toán hệ thống (AuditLogService).\n- Kiểm thử hệ thống, đo lường hiệu năng và viết báo cáo BTL.'
    t_assign.rows[2].cells[3].paragraphs[0].text = '100%'

    for r in t_assign.rows:
        for c in r.cells:
            for p in c.paragraphs:
                p.paragraph_format.line_spacing = 1.5
                for run in p.runs:
                    run.font.name = 'Times New Roman'
                    run.font.size = Pt(13)
                    run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)
            
    doc.add_page_break()

    # ==========================
    # 4. DANH MỤC HÌNH ẢNH, BẢNG BIỂU, CHỮ VIẾT TẮT
    # ==========================
    add_heading_1('DANH MỤC HÌNH ẢNH')
    fig_list = [
        'Hình 1.1: Sơ đồ kiến trúc tổng thể 3 tầng ứng dụng WPF và MySQL Cloud',
        'Hình 2.1: Mô hình kiến trúc Model - View - ViewModel (MVVM) áp dụng trong hệ thống',
        'Hình 2.2: Sơ đồ Use Case tổng quát hệ thống quản lý quán Billiard',
        'Hình 2.3: Sơ đồ tuần tự nghiệp vụ mở bàn và tính tiền giờ',
        'Hình 2.4: Sơ đồ tuần tự nghiệp vụ chuyển bàn và chia khoảng tính giá',
        'Hình 2.5: Sơ đồ quan hệ thực thể cơ sở dữ liệu (ERD) 17 bảng',
        'Hình 2.6: Sơ đồ luồng xác thực đăng nhập và kiểm tra phân quyền RBAC',
        'Hình 2.7: Bản vẽ thiết kế giao diện Màn hình Đăng nhập (Login Window)',
        'Hình 2.8: Bản vẽ thiết kế giao diện Màn hình Sơ đồ bàn (Table Management View)',
        'Hình 2.9: Bản vẽ thiết kế giao diện Màn hình Quản lý nhân viên (Staff Management View)',
        'Hình 3.1: Sơ đồ kiến trúc liên kết bảo mật cơ sở dữ liệu với connections.config',
        'Hình 3.2: Biểu đồ kết quả kiểm thử tải và thời gian đáp ứng cơ sở dữ liệu đám mây'
    ]
    for f in fig_list:
        add_para(f, space_after=3)
        
    doc.add_paragraph().paragraph_format.space_before = Pt(12)
    add_heading_1('DANH MỤC BẢNG BIỂU')
    table_list = [
        'Bảng 0.1: Phân công nhiệm vụ thực hiện đề tài',
        'Bảng 1.1: Bảng đối chiếu giải pháp vận hành thủ công và ứng dụng phần mềm Billiard Pro',
        'Bảng 2.1: Bảng danh từ điển dữ liệu - Nhóm bảng Tài khoản và Phân quyền (Roles, Users, Permissions, RolePermissions)',
        'Bảng 2.2: Bảng danh từ điển dữ liệu - Nhóm bảng Quản lý bàn (TableTypes, Tables)',
        'Bảng 2.3: Bảng danh từ điển dữ liệu - Nhóm bảng Dịch vụ F&B (ProductCategories, Products)',
        'Bảng 2.4: Bảng danh từ điển dữ liệu - Nhóm bảng Phiên chơi và Hóa đơn (Sessions, SessionProducts, Invoices, InvoiceDetails)',
        'Bảng 2.5: Bảng danh từ điển dữ liệu - Nhóm bảng Quy tắc tính giá và Chi tiết giờ (TablePricingRules, SessionPricingDetails)',
        'Bảng 2.6: Bảng danh từ điển dữ liệu - Nhóm Khách hàng, Nhân viên và Nhật ký kiểm toán (Customers, Employees, AuditLogs)',
        'Bảng 2.7: Bảng ma trận phân quyền hệ thống theo vai trò người dùng (RBAC Matrix)',
        'Bảng 3.1: Bảng tổng hợp công nghệ và công cụ phát triển phần mềm',
        'Bảng 3.2: Bảng kết quả thực hiện các ca kiểm thử chức năng trọng yếu (Test Cases)'
    ]
    for t in table_list:
        add_para(t, space_after=3)

    doc.add_paragraph().paragraph_format.space_before = Pt(12)
    add_heading_1('DANH MỤC CHỮ VIẾT TẮT')
    add_table_title('Bảng 0.2: Danh mục thuật ngữ và chữ viết tắt')
    t_abbr = doc.add_table(rows=9, cols=3)
    t_abbr.alignment = WD_TABLE_ALIGNMENT.CENTER
    abbr_data = [
        ('Chữ viết tắt', 'Thuật ngữ tiếng Anh', 'Ý nghĩa tiếng Việt'),
        ('ADO.NET', 'ActiveX Data Objects .NET', 'Công nghệ truy cập dữ liệu trên nền tảng .NET'),
        ('BTL', 'Bài tập lớn', 'Bài tập lớn học phần'),
        ('CRUD', 'Create, Read, Update, Delete', 'Bốn thao tác dữ liệu cơ bản: Thêm, Đọc, Sửa, Xóa'),
        ('CSDL', 'Cơ sở dữ liệu', 'Hệ thống cơ sở dữ liệu lưu trữ'),
        ('ERD', 'Entity Relationship Diagram', 'Sơ đồ quan hệ thực thể'),
        ('F&B', 'Food and Beverage', 'Dịch vụ ăn uống, giải khát phục vụ tại quán'),
        ('MVVM', 'Model - View - ViewModel', 'Mô hình kiến trúc phần mềm tách biệt giao diện và logic'),
        ('RBAC', 'Role-Based Access Control', 'Mô hình kiểm soát truy cập dựa trên vai trò')
    ]
    for row_idx, r_data in enumerate(abbr_data):
        for col_idx, text in enumerate(r_data):
            c = t_abbr.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(13)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)
            
    doc.add_page_break()

    # ==========================
    # 5. MỞ ĐẦU
    # ==========================
    add_heading_1('MỞ ĐẦU')
    
    add_para('1. Lý do chọn đề tài', bold=True)
    add_para('Trong những năm gần đây, phong trào thể thao giải trí Billiard (Bida) tại Việt Nam phát triển vô cùng mạnh mẽ, chuyển biến từ các quán nhỏ lẻ mang tính tự phát sang các Câu lạc bộ (CLB) quy mô từ 10 đến hơn 50 bàn với nhiều thể loại như Carom (Libre, 3 Băng), Pool (Lỗ), Snooker. Đi kèm với quy mô mở rộng là sự phức tạp trong hoạt động vận hành: tính tiền giờ linh hoạt theo loại bàn, khung giờ vàng, ngày cuối tuần, chuyển đổi bàn chơi khi khách yêu cầu, gọi đồ uống/thức ăn trực tiếp vào bàn và quản lý giao ca đối soát dòng tiền.')
    add_para('Tuy nhiên, phần lớn các quán bida vừa và nhỏ hiện nay vẫn quản lý bằng sổ tay hoặc các bảng tính Excel rời rạc. Phương thức này bộc lộ rất nhiều nhược điểm nghiêm trọng: nhân viên quên ghi giờ bắt đầu, sai sót khi tính tiền lũy tiến hoặc chuyển bàn, thất thoát tiền hàng F&B, và chủ quán hoàn toàn thiếu công cụ giám sát từ xa theo thời gian thực. Vì vậy, việc nghiên cứu và xây dựng một hệ thống phần mềm quản lý quán Billiard chuyên nghiệp trên nền tảng công nghệ hiện đại là một nhu cầu cấp thiết.')

    add_para('2. Mục đích nghiên cứu', bold=True)
    add_para('Mục đích cốt lõi của đề tài là áp dụng toàn diện kiến thức học phần Công nghệ .NET, mô hình kiến trúc Model - View - ViewModel (MVVM), hệ quản trị cơ sở dữ liệu MySQL và các nguyên lý bảo mật để xây dựng thành công phần mềm Quản lý Quán Billiard (Billiard Pro). Hệ thống hướng tới tự động hóa toàn bộ quy trình tính tiền giờ, quản lý dịch vụ F&B, kiểm soát phân quyền chặt chẽ và lưu vết mọi hoạt động nhằm triệt tiêu thất thoát tài chính.')

    add_para('3. Đối tượng và phạm vi nghiên cứu', bold=True)
    add_para('• Đối tượng nghiên cứu: Nghiệp vụ vận hành tại các câu lạc bộ Billiard thực tế (quản lý bàn, theo dõi phiên chơi, áp dụng chính sách giá, gọi món, thanh toán hóa đơn, phân quyền nhân sự).')
    add_para('• Phạm vi nghiên cứu: Ứng dụng Desktop chạy trên hệ điều hành Windows sử dụng nền tảng C# WPF (.NET Framework 4.7.2), kết nối cơ sở dữ liệu quan hệ MySQL 8.0 lưu trữ trên nền tảng đám mây Aiven Cloud thông qua giao thức bảo mật SSL.')

    add_para('4. Ý nghĩa khoa học và thực tiễn của đề tài', bold=True)
    add_para('• Ý nghĩa khoa học: Minh chứng cho tính hiệu quả của mô hình kiến trúc MVVM trong các ứng dụng desktop đòi hỏi giao diện phản hồi nhanh và ràng buộc dữ liệu thời gian thực (real-time data binding). Đề xuất cấu trúc cơ sở dữ liệu tối ưu với giải pháp phân đoạn thời gian (Time Slice) trong bài toán chuyển bàn và tính giá động.')
    add_para('• Ý nghĩa thực tiễn: Cung cấp một sản phẩm phần mềm hoàn chỉnh, có khả năng áp dụng trực tiếp vào các quán bida thực tế, giúp tiết kiệm thời gian vận hành, nâng cao tính chuyên nghiệp, minh bạch hóa hóa đơn đối với khách hàng và kiểm soát chặt chẽ doanh thu cho người quản lý.')

    doc.add_page_break()

    # ==========================
    # 6. CHƯƠNG 1: XÁC ĐỊNH YÊU CẦU
    # ==========================
    add_heading_1('CHƯƠNG 1: XÁC ĐỊNH YÊU CẦU')
    
    add_heading_2('1.1. Bối cảnh và đối tượng khảo sát')
    add_heading_3('1.1.1. Bối cảnh thị trường và mô hình kinh doanh câu lạc bộ Billiard')
    add_para('Thị trường dịch vụ giải trí Billiard tại Việt Nam đang có tốc độ tăng trưởng nhanh chóng. Một câu lạc bộ Billiard điển hình thường trang bị từ 10 đến 30 bàn chơi, phân chia thành nhiều khu vực chuyên biệt như: bàn Pool tiêu chuẩn (lỗ), bàn Carom Libre (phăng) và bàn Carom 3 Băng (bàn sưởi cao cấp). Khách hàng đến quán có hành vi chơi linh hoạt: có thể chơi đơn lẻ, chơi theo nhóm, chuyển từ bàn nhỏ sang bàn thi đấu lớn khi có người quen tham gia, và liên tục gọi các mặt hàng dịch vụ như nước suối, nước ngọt, bia, đồ ăn nhẹ hoặc thuốc lá [1].')

    add_heading_3('1.1.2. Khảo sát thực trạng quy trình vận hành truyền thống')
    add_para('Qua khảo sát thực tế tại các câu lạc bộ bida khu vực Hà Nội, quy trình vận hành truyền thống bộc lộ các công đoạn như sau: khi khách vào bàn, nhân viên ghi giờ vào giấy note hoặc sổ; khi khách gọi nước ngọt, nhân viên mang ra và ghi thêm một dòng vào phiếu bàn; khi khách thanh toán, nhân viên dùng máy tính cầm tay tính tổng số phút chơi nhân với đơn giá giờ, cộng với tiền nước rồi thu tiền mặt. Quy trình này phụ thuộc 100% vào sự cẩn thận và tính trung thực của nhân viên thu ngân.')

    add_heading_2('1.2. Xác định vấn đề cần giải quyết')
    add_heading_3('1.2.1. Những bất cập, thất thoát trong ghi nhận thủ công và tính tiền giờ')
    add_para('Việc tính tiền thủ công đối mặt với các vấn đề nhức nhối sau:')
    add_para('• Sai sót giờ chơi: Khi quán đông khách vào các giờ cao điểm (19h00 - 23h00), nhân viên thường quên ghi giờ vào hoặc ghi ước lượng, dẫn đến tranh cãi với khách hàng khi tính tiền.')
    add_para('• Bài toán chuyển bàn (Switch Table): Khi khách đang chơi ở bàn Libre giá 45.000,00 VNĐ/giờ muốn chuyển sang bàn 3 Băng giá 80.000,00 VNĐ/giờ, nhân viên rất khó tính chính xác số phút của từng bàn nếu không có phần mềm tự động chốt và cộng dồn.')
    add_para('• Gian lận giờ chơi: Nhân viên có thể cố tình bấm giờ trễ hoặc tính bớt tiền cho bạn bè quen biết mà chủ quán không có cách nào phát hiện.')

    add_heading_3('1.2.2. Khó khăn trong quản lý menu F&B, kiểm soát ca trực và đối soát dòng tiền')
    add_para('Các mặt hàng ăn uống đóng góp từ 30% đến 45% lợi nhuận của quán bida. Khi không có hệ thống quản lý, việc thất thoát đồ uống xảy ra thường xuyên: nhân viên lấy đồ uống cho khách nhưng không ghi vào hóa đơn, hoặc thu tiền khách ngoài sổ sách. Khi hết ca làm việc, việc bàn giao giữa ca sáng và ca chiều gặp khó khăn do không đối soát được số tiền mặt trong két và doanh thu thực tế [2].')

    add_table_title('Bảng 1.1: Bảng đối chiếu giải pháp vận hành thủ công và ứng dụng phần mềm Billiard Pro')
    t_comp = doc.add_table(rows=6, cols=3)
    t_comp.alignment = WD_TABLE_ALIGNMENT.CENTER
    comp_data = [
        ('Tiêu chí so sánh', 'Vận hành thủ công (Sổ sách / Excel)', 'Hệ thống phần mềm Billiard Pro'),
        ('Tính tiền giờ', 'Nhẩm tính hoặc máy tính cầm tay, dễ sai sót', 'Bấm giờ tự động theo từng giây, tính chuẩn theo phút'),
        ('Chuyển bàn chơi', 'Rất phức tạp, dễ nhầm lẫn khoảng giá', 'Tự động chốt Time Slice bàn cũ, chuyển mượt sang bàn mới'),
        ('Quản lý dịch vụ F&B', 'Ghi phiếu giấy rời rạc, dễ thất thoát đồ', 'Tích hợp trực tiếp vào bàn, trừ kho và chốt hóa đơn'),
        ('Bảo mật & Phân quyền', 'Không có phân quyền, ai cũng sửa được sổ', 'Phân quyền RBAC nghiêm ngặt: Admin, Thu ngân, Phục vụ'),
        ('Kiểm toán & Đối soát', 'Không có dấu vết khi bị sửa hoặc xóa', 'Lưu vết toàn bộ thao tác vào bảng AuditLogs không thể xóa')
    ]
    for row_idx, r_data in enumerate(comp_data):
        for col_idx, text in enumerate(r_data):
            c = t_comp.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(13)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    add_heading_2('1.3. Yêu cầu chung đối với hệ thống')
    add_heading_3('1.3.1. Phạm vi bài toán và đối tượng sử dụng hệ thống')
    add_para('Hệ thống Billiard Pro phục vụ hai nhóm đối tượng chính:')
    add_para('• Quản trị viên (Admin / Chủ quán): Có quyền truy cập toàn bộ hệ thống, quản lý cơ cấu danh mục bàn, thiết lập bảng giá theo giờ và chính sách khuyến mãi, quản lý danh mục sản phẩm F&B, quản lý tài khoản nhân viên và phân quyền chi tiết, xem báo cáo doanh thu tài chính.')
    add_para('• Nhân viên thu ngân (Staff / Cashier): Được cấp quyền mở bàn, tạm dừng bàn, chuyển bàn, gọi đồ uống/đồ ăn vào phiên chơi của bàn, in phiếu thanh toán và thu tiền khách hàng.')

    add_heading_3('1.3.2. Mô hình kiến trúc tổng thể và định hướng công nghệ')
    add_para('Hệ thống được thiết kế theo mô hình kiến trúc phân tầng (Tiered Architecture), đảm bảo tính tách biệt độc lập giữa tầng trình diễn, tầng nghiệp vụ và tầng lưu trữ dữ liệu. Toàn bộ cơ chế kết nối và trao đổi dữ liệu được thể hiện trong hình 1.1.')
    
    add_image_safe('docs/figures/fig_1_1_architecture.png')
    add_fig_title('Hình 1.1: Sơ đồ kiến trúc tổng thể 3 tầng ứng dụng WPF và MySQL Cloud')

    add_heading_2('1.4. Yêu cầu chức năng và phi chức năng chi tiết')
    add_heading_3('1.4.1. Yêu cầu chức năng phân hệ quản lý bàn và tính tiền giờ')
    add_para('• Hiển thị trực quan trạng thái bàn chơi thời gian thực: Bàn trống (Available), Đang có khách chơi (Occupied), Bàn tạm bảo trì (Maintenance).')
    add_para('• Chức năng Mở bàn (Start Session): Nhân viên chọn bàn trống và nhấn Mở bàn. Hệ thống tự động ghi nhận thời điểm bắt đầu (StartTime) chính xác đến từng giây.')
    add_para('• Chức năng Chuyển bàn (Switch Table): Cho phép chuyển phiên chơi hiện tại từ bàn A sang bàn B trống. Hệ thống tự động chốt tiền giờ của bàn A theo mức giá tương ứng, cập nhật bàn A về trạng thái Trống và gán phiên tiếp tục sang bàn B.')
    add_para('• Chức năng Tính tiền giờ tự động: Áp dụng bảng giá linh hoạt theo loại bàn (Pool, Libre, 3 Băng), hỗ trợ cấu hình theo ngày thường (Weekday) và cuối tuần (Weekend).')

    add_heading_3('1.4.2. Yêu cầu chức năng phân hệ dịch vụ, F&B và quản lý hóa đơn')
    add_para('• Quản lý danh mục sản phẩm F&B: Phân loại theo Nước giải khát, Đồ ăn nhanh, Thuốc lá, Phụ kiện thuê.')
    add_para('• Gọi món vào bàn: Cho phép nhân viên thêm món với số lượng tùy chọn vào bàn đang hoạt động. Đơn giá món được chụp lại (Snapshot) tại thời điểm gọi để đảm bảo giá không bị ảnh hưởng nếu sau này bảng giá sản phẩm thay đổi.')
    add_para('• Thanh toán và xuất hóa đơn: Tự động tổng hợp tiền giờ và tiền dịch vụ, cho phép nhập chiết khấu/giảm giá (DiscountAmount) và chọn hình thức thanh toán (Tiền mặt hoặc Chuyển khoản ngân hàng VietQR).')

    add_heading_3('1.4.3. Yêu cầu chức năng phân quyền, quản lý nhân sự và nhật ký hệ thống')
    add_para('• Quản lý tài khoản: Thêm mới, chỉnh sửa thông tin cá nhân, đặt lại mật khẩu và khóa/mở khóa tài khoản nhân viên.')
    add_para('• Phân quyền linh hoạt theo RBAC: Cấp hoặc thu hồi các quyền hạn độc lập như xem bàn, mở bàn, gọi món, thanh toán, xem báo cáo, quản lý nhân sự.')
    add_para('• Nhật ký kiểm toán (Audit Trail): Mọi thao tác trọng yếu đều được tự động ghi lại bao gồm: mã tài khoản thực hiện, hành vi thao tác, bảng dữ liệu bị tác động, thời điểm thực hiện và giá trị thay đổi dạng JSON.')

    add_heading_3('1.4.4. Yêu cầu phi chức năng (hiệu năng, an toàn dữ liệu, tính khả dụng)')
    add_para('• Thời gian đáp ứng: Các thao tác mở bàn, gọi món và chuyển trạng thái phải phản hồi trong thời gian dưới 100 miligiây trên mạng nội bộ hoặc dưới 250 miligiây khi kết nối tới máy chủ đám mây.')
    add_para('• Tính toàn vẹn và an toàn dữ liệu: Áp dụng Database Transaction đối với các giao dịch phức tạp (chuyển bàn, thanh toán hóa đơn). Mật khẩu người dùng phải được mã hóa một chiều bằng thuật toán BCrypt kèm muối ngẫu nhiên (Salt).')
    add_para('• Tính khả dụng: Giao diện trực quan, hỗ trợ hiển thị danh sách dạng lưới (Grid), các phím tắt tiện lợi cho thu ngân thao tác nhanh bằng bàn phím.')

    doc.add_page_break()

    # ==========================
    # 7. CHƯƠNG 2: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG
    # ==========================
    add_heading_1('CHƯƠNG 2: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG')
    
    add_heading_2('2.1. Phân tích yêu cầu và định hướng giải pháp kỹ thuật')
    add_heading_3('2.1.1. Lựa chọn kiến trúc phần mềm MVVM (Model - View - ViewModel)')
    add_para('Để xây dựng ứng dụng Windows Presentation Foundation (WPF) có khả năng bảo trì cao, hệ thống áp dụng triệt để mô hình kiến trúc MVVM. Mô hình này giúp loại bỏ hoàn toàn sự phụ thuộc trực tiếp giữa giao diện người dùng (XAML) và mã xử lý logic (Code-Behind) [3].')
    add_para('• Model: Đại diện cho các đối tượng thực thể trong hệ thống như `BilliardTable`, `BilliardSession`, `Product`, `Invoice`. Model chỉ chứa các thuộc tính dữ liệu và quy tắc nghiệp vụ cơ bản, độc lập với giao diện.')
    add_para('• View: Định nghĩa toàn bộ giao diện bằng ngôn ngữ khai báo XAML. View không chứa mã xử lý nghiệp vụ mà liên kết trực tiếp với ViewModel thông qua cơ chế Data Binding hai chiều (Two-Way Binding).')
    add_para('• ViewModel: Đóng vai trò cầu nối, hiện thực hóa giao diện `INotifyPropertyChanged` để tự động thông báo cho View cập nhật mỗi khi dữ liệu thay đổi, đồng thời cung cấp các lệnh `ICommand` (thông qua lớp `RelayCommand` và `AsyncRelayCommand`) để tiếp nhận tương tác người dùng. Kiến trúc MVVM được minh họa chi tiết trong hình 2.1.')

    add_image_safe('docs/figures/fig_2_1_mvvm.png')
    add_fig_title('Hình 2.1: Mô hình kiến trúc Model - View - ViewModel (MVVM) áp dụng trong hệ thống')

    add_heading_3('2.1.2. Giải pháp cơ sở dữ liệu quan hệ kết nối đám mây (Cloud Managed Database)')
    add_para('Thay vì cài đặt máy chủ CSDL MySQL cục bộ trên máy tính thu ngân vốn tiềm ẩn nguy cơ mất dữ liệu khi máy hỏng hóc hoặc nhiễm virus, đề tài lựa chọn giải pháp Cơ sở dữ liệu đám mây được quản lý (Aiven Managed MySQL Cloud). Giải pháp này đảm bảo dữ liệu luôn được sao lưu liên tục, sẵn sàng kết nối đa điểm và bảo mật bằng giao thức SSL/TLS 1.3.')

    add_heading_2('2.2. Thiết kế chức năng hệ thống')
    add_heading_3('2.2.1. Biểu đồ Use Case tổng quát và phân rã các tác tử')
    add_para('Dựa trên kết quả khảo sát yêu cầu ở Chương 1, hệ thống gồm 2 tác tử chính là Nhân viên (Staff) và Quản trị viên (Admin). Sơ đồ Use Case tổng quát được trình bày trong hình 2.2.')

    add_image_safe('docs/figures/fig_2_2_usecase.png')
    add_fig_title('Hình 2.2: Sơ đồ Use Case tổng quát hệ thống quản lý quán Billiard')

    add_heading_3('2.2.2. Mô tả chi tiết các Use Case chính')
    add_para('1. Use Case UC01: Đăng nhập & Xác thực hệ thống', bold=True)
    add_para('• Tác tử: Nhân viên, Quản trị viên.')
    add_para('• Điều kiện tiên quyết: Người dùng đã được cấp tài khoản hoạt động trong CSDL.')
    add_para('• Luồng sự kiện chính: Người dùng nhập Username và Password trên màn hình `LoginWindow`. Hệ thống kiểm tra chuỗi băm mật khẩu bằng thuật toán BCrypt. Nếu chính xác, hệ thống nạp toàn bộ danh sách mã quyền tương ứng vào `UserSession.Permissions`, ghi nhật ký đăng nhập và mở `MainWindow`.')

    add_para('2. Use Case UC02: Mở phiên chơi tại bàn (Start Session)', bold=True)
    add_para('• Tác tử: Nhân viên thu ngân.')
    add_para('• Điều kiện tiên quyết: Bàn được chọn phải ở trạng thái Trống (Available).')
    add_para('• Luồng sự kiện chính: Nhân viên bấm chọn bàn và thực thi lệnh mở bàn. Tầng nghiệp vụ gọi phương thức `StartSessionAsync` trong `TableService` để thêm bản ghi mới vào bảng `Sessions`, đồng thời cập nhật trạng thái bàn sang `Occupied`. Trình tự giao tiếp được thể hiện chi tiết trong hình 2.3.')

    add_image_safe('docs/figures/fig_2_3_seq_open_table.png')
    add_fig_title('Hình 2.3: Sơ đồ tuần tự nghiệp vụ mở bàn và tính tiền giờ')

    add_para('3. Use Case UC04: Chuyển bàn chơi (Switch Table)', bold=True)
    add_para('• Tác tử: Nhân viên thu ngân.')
    add_para('• Điều kiện tiên quyết: Bàn hiện tại đang chơi (Occupied) và bàn đích đang trống (Available).')
    add_para('• Luồng sự kiện chính: Hệ thống thực hiện một Database Transaction toàn vẹn: tính toán số phút đã chơi tại bàn cũ và chốt một khoảng thời gian (Time Slice) ghi vào bảng `SessionPricingDetails`; chuyển trạng thái bàn cũ về `Available`; chuyển trạng thái bàn đích sang `Occupied`; cập nhật mã bàn mới vào bản ghi `Sessions`. Sơ đồ tuần tự được mô tả trong hình 2.4.')

    add_image_safe('docs/figures/fig_2_4_seq_switch_table.png')
    add_fig_title('Hình 2.4: Sơ đồ tuần tự nghiệp vụ chuyển bàn và chia khoảng tính giá')

    add_heading_3('2.2.3. Thiết kế luồng xử lý và thuật toán tính giá giờ lũy tiến / theo khung giờ')
    add_para('Công thức tính tiền giờ cho một phiên chơi tổng quát được tính bằng tổng thành tiền của các khoảng thời gian tính giá (Time Slices):')
    add_para('Tiền giờ bàn = Tổng các khoảng [ (Thời gian chơi trong khoảng / 60) * Đơn giá giờ thực tế ]', italic=True)
    add_para('Trong đó, đơn giá giờ thực tế được xác định từ bảng `TablePricingRules` dựa trên loại bàn (TableTypeId), ngày trong tuần (Weekday/Weekend) và mức chiết khấu khuyến mãi (nếu có).')

    add_heading_2('2.3. Thiết kế cơ sở dữ liệu')
    add_heading_3('2.3.1. Sơ đồ quan hệ thực thể (ERD) và các ràng buộc toàn vẹn')
    add_para('Cơ sở dữ liệu của hệ thống được chuẩn hóa đạt chuẩn dạng chuẩn 3 (3NF), gồm 17 bảng liên kết chặt chẽ với nhau nhằm triệt tiêu dữ liệu trùng lặp và đảm bảo tính toàn vẹn khóa ngoại (Foreign Key Constraints). Sơ đồ quan hệ thực thể tổng thể được thể hiện trong hình 2.5.')

    add_image_safe('docs/figures/fig_2_5_erd.png')
    add_fig_title('Hình 2.5: Sơ đồ quan hệ thực thể cơ sở dữ liệu (ERD) 17 bảng')

    add_heading_3('2.3.2. Chi tiết cấu trúc 17 bảng dữ liệu')
    add_para('Cấu trúc chi tiết của 17 bảng dữ liệu được phân chia theo 6 phân hệ nghiệp vụ chính từ bảng 2.1 đến bảng 2.6.')

    # Table 2.1
    add_table_title('Bảng 2.1: Bảng danh từ điển dữ liệu - Nhóm bảng Tài khoản và Phân quyền (Roles, Users, Permissions, RolePermissions)')
    t_db1 = doc.add_table(rows=8, cols=4)
    t_db1.alignment = WD_TABLE_ALIGNMENT.CENTER
    db1_data = [
        ('Tên bảng', 'Tên trường', 'Kiểu dữ liệu & Ràng buộc', 'Ý nghĩa nghiệp vụ'),
        ('Roles', 'RoleId\nRoleName\nDescription', 'INT, PK, AUTO_INCREMENT\nVARCHAR(30), UNIQUE, NOT NULL\nVARCHAR(255), NULL', 'Mã vai trò\nTên vai trò (Admin, Staff)\nMô tả vai trò'),
        ('Users', 'UserId\nRoleId\nUsername\nPasswordHash\nFullName\nPhone\nIsActive\nCreatedAt', 'BIGINT, PK, AUTO_INCREMENT\nINT, FK -> Roles.RoleId\nVARCHAR(50), UNIQUE, NOT NULL\nVARCHAR(255), NOT NULL\nVARCHAR(100), NOT NULL\nVARCHAR(20), NULL\nBOOLEAN, DEFAULT TRUE\nDATETIME, DEFAULT NOW()', 'Mã tài khoản\nVai trò người dùng\nTên đăng nhập\nMật khẩu băm BCrypt\nHọ và tên hiển thị\nSố điện thoại\nTrạng thái hoạt động\nThời điểm tạo tài khoản'),
        ('Permissions', 'PermissionId\nPermissionCode\nPermissionName\nDescription', 'INT, PK, AUTO_INCREMENT\nVARCHAR(50), UNIQUE, NOT NULL\nVARCHAR(100), NOT NULL\nVARCHAR(255), NULL', 'Mã quyền\nMã code duy nhất (TABLE_VIEW...)\nTên quyền hiển thị\nMô tả chi tiết quyền'),
        ('RolePermissions', 'RoleId\nPermissionId', 'INT, PK ghép, FK -> Roles\nINT, PK ghép, FK -> Permissions', 'Khóa ngoại bảng Roles\nKhóa ngoại bảng Permissions')
    ]
    # Populate Table 2.1
    curr_r = 0
    for row_idx, r_data in enumerate(db1_data):
        for col_idx, text in enumerate(r_data):
            c = t_db1.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    # Table 2.2
    add_table_title('Bảng 2.2: Bảng danh từ điển dữ liệu - Nhóm bảng Quản lý bàn (TableTypes, Tables)')
    t_db2 = doc.add_table(rows=3, cols=4)
    t_db2.alignment = WD_TABLE_ALIGNMENT.CENTER
    db2_data = [
        ('Tên bảng', 'Tên trường', 'Kiểu dữ liệu & Ràng buộc', 'Ý nghĩa nghiệp vụ'),
        ('TableTypes', 'TableTypeId\nTypeName\nDescription\nIsActive', 'INT, PK, AUTO_INCREMENT\nVARCHAR(50), UNIQUE, NOT NULL\nVARCHAR(255), NULL\nBOOLEAN, DEFAULT TRUE', 'Mã loại bàn\nTên loại (Pool, Libre, 3 Băng)\nMô tả loại bàn\nTrạng thái hoạt động'),
        ('Tables', 'TableId\nTableTypeId\nTableNumber\nStatus\nIsActive', 'INT, PK, AUTO_INCREMENT\nINT, FK -> TableTypes, NOT NULL\nVARCHAR(20), UNIQUE, NOT NULL\nVARCHAR(20), DEFAULT \'Available\'\nBOOLEAN, DEFAULT TRUE', 'Mã bàn chơi thực tế\nKhóa ngoại loại bàn\nSố hiệu bàn (P01, C01, 3B01)\nTrạng thái (Available/Occupied)\nĐang kinh doanh hay không')
    ]
    for row_idx, r_data in enumerate(db2_data):
        for col_idx, text in enumerate(r_data):
            c = t_db2.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    # Table 2.3
    add_table_title('Bảng 2.3: Bảng danh từ điển dữ liệu - Nhóm bảng Dịch vụ F&B (ProductCategories, Products)')
    t_db3 = doc.add_table(rows=3, cols=4)
    t_db3.alignment = WD_TABLE_ALIGNMENT.CENTER
    db3_data = [
        ('Tên bảng', 'Tên trường', 'Kiểu dữ liệu & Ràng buộc', 'Ý nghĩa nghiệp vụ'),
        ('ProductCategories', 'CategoryId\nCategoryName\nDescription\nIsActive', 'INT, PK, AUTO_INCREMENT\nVARCHAR(100), UNIQUE, NOT NULL\nVARCHAR(255), NULL\nBOOLEAN, DEFAULT TRUE', 'Mã danh mục\nTên phân loại F&B\nMô tả chi tiết\nTrạng thái kích hoạt'),
        ('Products', 'ProductId\nCategoryId\nProductName\nUnit\nPrice\nImageUrl\nIsActive', 'INT, PK, AUTO_INCREMENT\nINT, FK -> ProductCategories\nVARCHAR(150), NOT NULL\nVARCHAR(30), NOT NULL\nDECIMAL(12,2), NOT NULL\nVARCHAR(2048), NULL\nBOOLEAN, DEFAULT TRUE', 'Mã sản phẩm\nDanh mục sản phẩm\nTên mặt hàng F&B\nĐơn vị tính (Lon, Chai, Tô)\nGiá bán niêm yết\nĐường dẫn ảnh sản phẩm\nTrạng thái kinh doanh')
    ]
    for row_idx, r_data in enumerate(db3_data):
        for col_idx, text in enumerate(r_data):
            c = t_db3.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    # Table 2.4
    add_table_title('Bảng 2.4: Bảng danh từ điển dữ liệu - Nhóm bảng Phiên chơi và Hóa đơn (Sessions, SessionProducts, Invoices, InvoiceDetails)')
    t_db4 = doc.add_table(rows=5, cols=4)
    t_db4.alignment = WD_TABLE_ALIGNMENT.CENTER
    db4_data = [
        ('Tên bảng', 'Tên trường', 'Kiểu dữ liệu & Ràng buộc', 'Ý nghĩa nghiệp vụ'),
        ('Sessions', 'SessionId\nTableId\nUserId\nCustomerId\nStartTime\nEndTime\nStatus\nTableAmount', 'BIGINT, PK, AUTO_INCREMENT\nINT, FK -> Tables\nBIGINT, FK -> Users\nBIGINT, FK -> Customers, NULL\nDATETIME, NOT NULL\nDATETIME, NULL\nVARCHAR(20), DEFAULT \'Active\'\nDECIMAL(12,2), DEFAULT 0.00', 'Mã phiên chơi\nBàn đang phục vụ\nNhân viên mở bàn\nKhách hàng chơi\nThời điểm bắt đầu\nThời điểm kết thúc\nTrạng thái phiên chơi\nTổng tiền giờ chốt được'),
        ('SessionProducts', 'SessionProductId\nSessionId\nProductId\nQuantity\nUnitPrice\nAmount\nCreatedAt', 'BIGINT, PK, AUTO_INCREMENT\nBIGINT, FK -> Sessions\nINT, FK -> Products\nINT, CHECK (Quantity > 0)\nDECIMAL(12,2), NOT NULL\nDECIMAL(12,2), NOT NULL\nDATETIME, DEFAULT NOW()', 'Mã dòng gọi món\nThuộc phiên chơi nào\nMặt hàng được gọi\nSố lượng gọi\nĐơn giá chụp tại lúc gọi\nThành tiền = SL * Đơn giá\nThời điểm gọi món'),
        ('Invoices', 'InvoiceId\nInvoiceCode\nSessionId\nUserId\nCreatedAt\nTableAmount\nServiceAmount\nDiscountAmount\nTotalAmount\nStatus\nPaymentMethod\nPaidAt', 'BIGINT, PK, AUTO_INCREMENT\nVARCHAR(30), UNIQUE, NOT NULL\nBIGINT, FK -> Sessions, UNIQUE\nBIGINT, FK -> Users\nDATETIME, DEFAULT NOW()\nDECIMAL(12,2), DEFAULT 0.00\nDECIMAL(12,2), DEFAULT 0.00\nDECIMAL(12,2), DEFAULT 0.00\nDECIMAL(12,2), NOT NULL\nVARCHAR(20), DEFAULT \'Pending\'\nVARCHAR(20), NULL\nDATETIME, NULL', 'Mã hóa đơn nội bộ\nMã hóa đơn hiển thị (HD...)\nKhóa ngoại phiên chơi (1-1)\nThu ngân xuất hóa đơn\nThời điểm xuất bill\nTổng tiền giờ bàn\nTổng tiền dịch vụ F&B\nSố tiền giảm giá chiết khấu\nTổng tiền thanh toán\nTrạng thái (Paid/Pending)\nHình thức (Cash/Bank)\nThời điểm thanh toán'),
        ('InvoiceDetails', 'InvoiceDetailId\nInvoiceId\nItemType\nProductId\nDescription\nQuantity\nUnitPrice\nAmount', 'BIGINT, PK, AUTO_INCREMENT\nBIGINT, FK -> Invoices\nVARCHAR(20), NOT NULL\nINT, FK -> Products, NULL\nVARCHAR(255), NOT NULL\nDECIMAL(10,2), NOT NULL\nDECIMAL(12,2), NOT NULL\nDECIMAL(12,2), NOT NULL', 'Mã dòng chi tiết\nThuộc hóa đơn nào\nLoại dòng (Table/Product)\nSản phẩm (nếu là món)\nNội dung hiển thị lên bill\nSố lượng hoặc số giờ\nĐơn giá chụp lại\nThành tiền của dòng')
    ]
    for row_idx, r_data in enumerate(db4_data):
        for col_idx, text in enumerate(r_data):
            c = t_db4.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    # Table 2.5
    add_table_title('Bảng 2.5: Bảng danh từ điển dữ liệu - Nhóm bảng Quy tắc tính giá và Chi tiết giờ (TablePricingRules, SessionPricingDetails)')
    t_db5 = doc.add_table(rows=3, cols=4)
    t_db5.alignment = WD_TABLE_ALIGNMENT.CENTER
    db5_data = [
        ('Tên bảng', 'Tên trường', 'Kiểu dữ liệu & Ràng buộc', 'Ý nghĩa nghiệp vụ'),
        ('TablePricingRules', 'PricingRuleId\nTableTypeId\nRuleName\nRuleType\nDayType\nStartTime\nEndTime\nHourlyRate\nDiscountType\nDiscountValue\nEffectiveFrom\nPriority\nIsActive', 'BIGINT, PK, AUTO_INCREMENT\nINT, FK -> TableTypes\nVARCHAR(100), NOT NULL\nVARCHAR(20), NOT NULL\nVARCHAR(20), NOT NULL\nTIME, NULL\nTIME, NULL\nDECIMAL(12,2), NULL\nVARCHAR(20), NULL\nDECIMAL(12,2), NULL\nDATE, NOT NULL\nINT, DEFAULT 1\nBOOLEAN, DEFAULT TRUE', 'Mã quy tắc giá\nÁp dụng cho loại bàn\nTên quy tắc\nLoại (BaseRate/Promotion)\nNgày (Weekday/Weekend)\nKhung giờ bắt đầu\nKhung giờ kết thúc\nĐơn giá gốc 1 giờ\nKiểu giảm (% / Cố định)\nMức giảm giá\nNgày bắt đầu áp dụng\nĐộ ưu tiên quy tắc\nTrạng thái kích hoạt'),
        ('SessionPricingDetails', 'SessionPricingDetailId\nSessionId\nTableId\nBasePricingRuleId\nStartTime\nEndTime\nDurationMinutes\nBaseHourlyRate\nFinalHourlyRate\nAmount', 'BIGINT, PK, AUTO_INCREMENT\nBIGINT, FK -> Sessions\nINT, FK -> Tables\nBIGINT, FK -> TablePricingRules\nDATETIME, NOT NULL\nDATETIME, NOT NULL\nDECIMAL(10,2), NOT NULL\nDECIMAL(12,2), NOT NULL\nDECIMAL(12,2), NOT NULL\nDECIMAL(12,2), NOT NULL', 'Mã khoảng tính giờ\nThuộc phiên chơi nào\nBàn chơi trong khoảng này\nQuy tắc giá được áp dụng\nBắt đầu khoảng thời gian\nKết thúc khoảng thời gian\nSố phút chơi trong khoảng\nGiá gốc 1 giờ\nGiá thực tế sau giảm\nTiền bàn = (Phút/60) * Giá')
    ]
    for row_idx, r_data in enumerate(db5_data):
        for col_idx, text in enumerate(r_data):
            c = t_db5.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    # Table 2.6
    add_table_title('Bảng 2.6: Bảng danh từ điển dữ liệu - Nhóm Khách hàng, Nhân viên và Nhật ký kiểm toán (Customers, Employees, AuditLogs)')
    t_db6 = doc.add_table(rows=4, cols=4)
    t_db6.alignment = WD_TABLE_ALIGNMENT.CENTER
    db6_data = [
        ('Tên bảng', 'Tên trường', 'Kiểu dữ liệu & Ràng buộc', 'Ý nghĩa nghiệp vụ'),
        ('Customers', 'CustomerId\nFullName\nPhone\nCreatedAt\nIsActive', 'BIGINT, PK, AUTO_INCREMENT\nVARCHAR(100), NOT NULL\nVARCHAR(20), NULL\nDATETIME, DEFAULT NOW()\nBOOLEAN, DEFAULT TRUE', 'Mã khách hàng\nHọ và tên khách quen\nSố điện thoại liên lạc\nNgày đăng ký hội viên\nTrạng thái hoạt động'),
        ('Employees', 'EmployeeId\nUserId\nEmail\nAddress\nHireDate\nPosition\nIsActive', 'BIGINT, PK, AUTO_INCREMENT\nBIGINT, FK -> Users, UNIQUE\nVARCHAR(100), NULL\nVARCHAR(255), NULL\nDATE, NULL\nVARCHAR(50), NULL\nBOOLEAN, DEFAULT TRUE', 'Mã hồ sơ nhân sự\nKhóa ngoại tài khoản (1-1)\nEmail nhân viên\nĐịa chỉ thường trú\nNgày bắt đầu vào làm\nVị trí công việc đảm nhiệm\nTrạng thái làm việc'),
        ('AuditLogs', 'AuditLogId\nUserId\nAction\nEntityType\nEntityId\nDescription\nOldValues\nNewValues\nCreatedAt', 'BIGINT, PK, AUTO_INCREMENT\nBIGINT, FK -> Users, NULL\nVARCHAR(50), NOT NULL\nVARCHAR(50), NOT NULL\nBIGINT, NULL\nVARCHAR(500), NULL\nJSON, NULL\nJSON, NULL\nDATETIME, DEFAULT NOW()', 'Mã nhật ký kiểm toán\nTài khoản thao tác\nHành động (OPEN, CLOSE...)\nThực thể bị tác động\nID bản ghi tương ứng\nDiễn giải chi tiết thao tác\nDữ liệu trước khi sửa\nDữ liệu sau khi sửa\nThời điểm ghi nhận log')
    ]
    for row_idx, r_data in enumerate(db6_data):
        for col_idx, text in enumerate(r_data):
            c = t_db6.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    add_heading_3('2.3.3. Thiết kế chiến lược lưu vết (Audit Trail) và snapshot giá')
    add_para('Một trong những thiết kế quan trọng nhất để chống gian lận trong Billiard Pro là cơ chế chụp lại giá (Snapshot) và nhật ký kiểm toán (Audit Trail):')
    add_para('• Snapshot đơn giá sản phẩm và tiền giờ: Khi gọi món hoặc chốt thời gian, đơn giá tại thời điểm đó lập tức được sao chép vào trường `UnitPrice` của bảng `SessionProducts` và `SessionPricingDetails`. Do đó, nếu người quản trị thay đổi giá niêm yết trong bảng `Products`, các hóa đơn đã phát sinh trong quá khứ hoàn toàn không bị sai lệch số liệu.')
    add_para('• Bảng lưu vết AuditLogs: Mỗi khi có thao tác nhạy cảm (mở bàn, sửa tài khoản, đổi quyền, hủy món, chuyển bàn), hệ thống tự động ghi nhận lại dưới dạng JSON gồm trạng thái cũ (`OldValues`) và trạng thái mới (`NewValues`).')

    add_heading_2('2.4. Thiết kế giao diện tương tác người dùng (UI/UX)')
    add_heading_3('2.4.1. Giao diện Đăng nhập và xác thực (Login Window)')
    add_para('Màn hình đăng nhập được thiết kế tinh gọn, tập trung với hai trường nhập liệu Username và Password, hỗ trợ ghi nhớ đăng nhập bằng tệp cấu hình máy trạm. Thiết kế bản vẽ wireframe được mô tả trong hình 2.7.')

    add_image_safe('docs/figures/fig_2_7_ui_login.png')
    add_fig_title('Hình 2.7: Bản vẽ thiết kế giao diện Màn hình Đăng nhập (Login Window)')

    add_heading_3('2.4.2. Giao diện Sơ đồ bàn thời gian thực (Table Management View)')
    add_para('Màn hình trung tâm của phần mềm hiển thị danh sách toàn bộ các bàn chơi dưới dạng các thẻ lưới (Cards). Mỗi bàn hiển thị rõ ràng: số hiệu bàn, loại bàn, trạng thái, thời gian đã chơi (được cập nhật từng giây bằng `DispatcherTimer`) và số tiền tạm tính hiện tại. Thiết kế giao diện được mô tả trong hình 2.8.')

    add_image_safe('docs/figures/fig_2_8_ui_table.png')
    add_fig_title('Hình 2.8: Bản vẽ thiết kế giao diện Màn hình Sơ đồ bàn (Table Management View)')

    add_heading_3('2.4.3. Giao diện Quản lý nhân viên và phân quyền (Staff Management View)')
    add_para('Màn hình quản lý nhân viên cho phép quản trị viên xem danh sách nhân sự bên trái và biểu mẫu chi tiết bên phải. Quản trị viên có thể cấp hoặc thu hồi trực tiếp 8 quyền hạn cốt lõi của hệ thống, như mô tả trong hình 2.9.')

    add_image_safe('docs/figures/fig_2_9_ui_staff.png')
    add_fig_title('Hình 2.9: Bản vẽ thiết kế giao diện Màn hình Quản lý nhân viên (Staff Management View)')

    add_heading_3('2.4.4. Giao diện Quản lý dịch vụ F&B và Tra cứu hóa đơn')
    add_para('Phân hệ dịch vụ F&B cho phép nhân viên tìm kiếm món ăn, đồ uống nhanh chóng theo tên hoặc danh mục, chỉ định số lượng và thêm vào bàn chơi với thao tác click chuột đơn giản. Phân hệ hóa đơn hỗ trợ xem lại lịch sử các phiên chơi, in phiếu tạm tính, in hóa đơn thanh toán chính thức và xuất dữ liệu đối soát cuối ca.')

    add_heading_2('2.5. Thiết kế bảo mật và kiểm soát truy cập')
    add_heading_3('2.5.1. Cơ chế mã hóa mật khẩu một chiều BCrypt và chống tấn công dò mật khẩu')
    add_para('Hệ thống kiên quyết không lưu mật khẩu ở dạng văn bản thuần (Plaintext). Mật khẩu được băm một chiều bằng thuật toán BCrypt với hệ số độ phức tạp (Work Factor) là 11, tự động sinh chuỗi muối ngẫu nhiên (Salt) cho mỗi người dùng. Ngay cả khi cơ sở dữ liệu bị trích xuất trái phép, kẻ tấn công cũng không thể khôi phục lại mật khẩu gốc bằng các bảng Rainbow Table thông thường.')

    add_heading_3('2.5.2. Mô hình phân quyền theo vai trò (Role-Based Access Control - RBAC)')
    add_para('Hệ thống áp dụng mô hình phân quyền ma trận 2 lớp (Role -> RolePermissions -> Permissions). Toàn bộ 8 quyền hệ thống được kiểm tra nghiêm ngặt cả ở tầng giao diện lẫn tầng nghiệp vụ (Service Layer) thông qua lớp trợ giúp `UserSession`. Luồng kiểm tra phân quyền được mô tả trong hình 2.6 và bảng phân quyền chi tiết trong bảng 2.7.')

    add_image_safe('docs/figures/fig_2_6_auth_flow.png')
    add_fig_title('Hình 2.6: Sơ đồ luồng xác thực đăng nhập và kiểm tra phân quyền RBAC')

    add_table_title('Bảng 2.7: Bảng ma trận phân quyền hệ thống theo vai trò người dùng (RBAC Matrix)')
    t_rbac = doc.add_table(rows=10, cols=4)
    t_rbac.alignment = WD_TABLE_ALIGNMENT.CENTER
    rbac_data = [
        ('Mã quyền (Permission Code)', 'Tên quyền hạn hiển thị', 'Quản trị viên (Admin)', 'Thu ngân (Staff)'),
        ('TABLE_VIEW', 'Xem sơ đồ và trạng thái bàn', 'Có', 'Có'),
        ('SESSION_OPEN', 'Mở phiên chơi và tính giờ bàn', 'Có', 'Có'),
        ('SESSION_CLOSE', 'Kết thúc phiên chơi và thanh toán', 'Có', 'Có'),
        ('PRODUCT_MANAGE', 'Quản lý thực đơn và gọi món F&B', 'Có', 'Có (Gọi món)'),
        ('INVOICE_PAY', 'Thu tiền và xuất hóa đơn thanh toán', 'Có', 'Có'),
        ('REPORT_VIEW', 'Xem báo cáo doanh thu & biểu đồ', 'Có', 'Không'),
        ('USER_MANAGE', 'Quản lý hồ sơ nhân viên và tài khoản', 'Có', 'Không'),
        ('ROLE_MANAGE', 'Cấu hình và gán quyền hệ thống', 'Có', 'Không'),
        ('AUDIT_VIEW', 'Xem nhật ký kiểm toán hệ thống', 'Có', 'Không')
    ]
    for row_idx, r_data in enumerate(rbac_data):
        for col_idx, text in enumerate(r_data):
            c = t_rbac.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    add_heading_3('2.5.3. Bảo mật chuỗi kết nối và kiểm soát kênh truyền SSL/TLS')
    add_para('Một lỗ hổng phổ biến của các đồ án sinh viên là lưu cứng chuỗi kết nối (chứa IP, User, Password) trực tiếp trong mã nguồn C# hoặc file `App.config` đưa lên GitHub. Để giải quyết triệt để vấn đề này, đề tài áp dụng cơ chế nạp cấu hình ngoại vi `configSource="connections.config"`. File `connections.config` được cấu hình trong file `.gitignore` để không bao giờ bị lộ ra ngoài kho lưu trữ mã nguồn.')

    doc.add_page_break()

    # ==========================
    # 8. CHƯƠNG 3: TRIỂN KHAI VÀ THỬ NGHIỆM
    # ==========================
    add_heading_1('CHƯƠNG 3: TRIỂN KHAI VÀ THỬ NGHIỆM')
    
    add_heading_2('3.1. Lựa chọn giải pháp công nghệ và môi trường phát triển')
    add_heading_3('3.1.1. Nền tảng phát triển .NET Framework 4.7.2, C# và WPF XAML')
    add_para('Dự án được xây dựng trên nền tảng Microsoft .NET Framework 4.7.2 kết hợp ngôn ngữ lập trình C# hiện đại. Công nghệ Windows Presentation Foundation (WPF) được lựa chọn nhờ khả năng tùy biến giao diện đồ họa vượt trội thông qua ngôn ngữ đánh dấu XAML, hỗ trợ tăng tốc phần cứng DirectX và cơ chế liên kết dữ liệu mạnh mẽ [4].')

    add_heading_3('3.1.2. Quản trị CSDL MySQL trên đám mây Aiven Cloud và kết nối MySqlConnector')
    add_para('Hệ thống sử dụng cơ sở dữ liệu MySQL phiên bản 8.0 lưu trữ trên cụm máy chủ đám mây Aiven. Kết nối từ client WPF tới cơ sở dữ liệu được thực thi thông qua thư viện hiệu năng cao `MySqlConnector` thay vì thư viện mặc định của Oracle, giúp tối ưu hóa việc phân bổ bộ nhớ đệm và hỗ trợ triệt để mô hình bất đồng bộ `async/await`.')

    add_heading_3('3.1.3. Thư viện giao diện Material Design Themes in XAML')
    add_para('Để mang lại trải nghiệm người dùng hiện đại, hệ thống tích hợp bộ thư viện mã nguồn mở `MaterialDesignThemes.Wpf`. Thư viện này cung cấp các điều khiển chuẩn hóa như PackIcon, Card, DialogHost, Snackbar và hệ thống màu sắc phẳng chuyên nghiệp.')

    add_table_title('Bảng 3.1: Bảng tổng hợp công nghệ và công cụ phát triển phần mềm')
    t_tech = doc.add_table(rows=7, cols=3)
    t_tech.alignment = WD_TABLE_ALIGNMENT.CENTER
    tech_data = [
        ('Thành phần hệ thống', 'Công nghệ / Thư viện lựa chọn', 'Phiên bản & Vai trò'),
        ('Ngôn ngữ lập trình', 'C# (C-Sharp)', 'Phiên bản 7.3+ trên .NET Framework 4.7.2'),
        ('Giao diện người dùng', 'WPF (Windows Presentation Foundation)', 'Thiết kế bố cục giao diện XAML & Material Design'),
        ('Kiến trúc phần mềm', 'MVVM (Model - View - ViewModel)', 'Tách biệt giao diện và logic, hỗ trợ Data Binding'),
        ('Hệ quản trị CSDL', 'MySQL Server 8.0 Cloud (Aiven)', 'Lưu trữ đám mây, mã hóa SSL TLS 1.3, Charset utf8mb4'),
        ('Trình điều khiển kết nối', 'MySqlConnector', 'Thư viện ADO.NET bất đồng bộ hiệu năng cao'),
        ('Bảo mật & Mã hóa', 'BCrypt.Net-Next', 'Mã hóa một chiều mật khẩu kèm muối ngẫu nhiên (Salt)')
    ]
    for row_idx, r_data in enumerate(tech_data):
        for col_idx, text in enumerate(r_data):
            c = t_tech.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    add_heading_2('3.2. Triển khai các thành phần mã nguồn trọng yếu')
    add_heading_3('3.2.1. Lớp kết nối dữ liệu dùng chung DatabaseHelper và cấu hình độc lập connections.config')
    add_para('Lớp `DatabaseHelper` quản lý việc mở và đóng các kết nối tới cơ sở dữ liệu một cách an toàn. Nhờ cơ chế `async/await` và mệnh đề `using`, kết nối được trả ngay về Connection Pool sau khi thực thi xong truy vấn, ngăn ngừa hiện tượng nghẽn tài nguyên máy chủ. Mô hình liên kết chuỗi kết nối an toàn được mô tả trong hình 3.1.')

    add_image_safe('docs/figures/fig_3_1_connection_security.png')
    add_fig_title('Hình 3.1: Sơ đồ kiến trúc liên kết bảo mật cơ sở dữ liệu với connections.config')

    add_heading_3('3.2.2. Triển khai tầng xử lý nghiệp vụ')
    add_para('Tầng nghiệp vụ (Service Layer) đóng gói toàn bộ logic cốt lõi:')
    add_para('• `TableService`: Cung cấp các hàm `GetAllTablesAsync`, `StartSessionAsync`, `SwitchTableAsync`, `UpdateTableStatusAsync`.')
    add_para('• `OrderService`: Cung cấp hàm tính tiền phiên chơi `CalculateSessionBillingAsync` và thanh toán xuất hóa đơn `CheckoutAsync`.')
    add_para('• `AuthService` & `UserService`: Xác thực đăng nhập, băm mật khẩu, truy vấn thông tin nhân sự và nạp phân quyền người dùng.')
    add_para('• `AuditLogService`: Ghi nhận nhật ký hệ thống độc lập trong các giao dịch nghiệp vụ.')

    add_heading_3('3.2.3. Triển khai cơ chế liên kết dữ liệu hai chiều (Data Binding) và Command trong MVVM')
    add_para('Để giao diện phản hồi mượt mà mà không gây treo màn hình (UI Freezing), tất cả các thao tác gọi dữ liệu từ xa đều được thực hiện thông qua lớp `AsyncRelayCommand`. Khi dữ liệu danh sách bàn hoặc trạng thái phiên chơi thay đổi, thuộc tính trong ViewModel kích hoạt sự kiện `PropertyChanged`, lập tức giao diện người dùng trên WPF được làm mới tự động mà không cần viết lệnh tải lại thủ công.')

    add_heading_2('3.3. Thử nghiệm hệ thống (Testing) và đánh giá vận hành')
    add_heading_3('3.3.1. Kế hoạch và các ca kiểm thử chức năng (Test Cases)')
    add_para('Hệ thống đã trải qua quá trình kiểm thử chức năng toàn diện với các ca kiểm thử chính được tổng hợp trong bảng 3.2.')

    add_table_title('Bảng 3.2: Bảng kết quả thực hiện các ca kiểm thử chức năng trọng yếu (Test Cases)')
    t_tc = doc.add_table(rows=8, cols=5)
    t_tc.alignment = WD_TABLE_ALIGNMENT.CENTER
    tc_data = [
        ('Mã TC', 'Chức năng kiểm thử', 'Dữ liệu đầu vào / Thao tác', 'Kết quả mong đợi', 'Đánh giá'),
        ('TC01', 'Đăng nhập thành công', 'User: admin, Mật khẩu: Admin@123', 'Đăng nhập thành công, nạp đủ 8 quyền, vào MainWindow', 'ĐẠT'),
        ('TC02', 'Đăng nhập sai mật khẩu', 'User: admin, Mật khẩu: SaiPass@123', 'Báo lỗi thông tin đăng nhập không hợp lệ, không nạp quyền', 'ĐẠT'),
        ('TC03', 'Mở bàn chơi', 'Chọn bàn P01 (Trống), bấm Mở bàn', 'Bàn đổi sang Occupied, hiển thị đồng hồ đếm thời gian', 'ĐẠT'),
        ('TC04', 'Gọi đồ uống F&B', 'Chọn bàn P01, thêm 2 lon Bò húc', 'Hệ thống lưu SessionProducts, cộng dồn tiền F&B', 'ĐẠT'),
        ('TC05', 'Chuyển bàn chơi', 'Chuyển phiên từ bàn P01 sang bàn C01', 'Chốt Time Slice bàn P01, P01 về Trống, C01 Đang chơi', 'ĐẠT'),
        ('TC06', 'Thanh toán hóa đơn', 'Chọn bàn P01, bấm Thanh toán', 'Tính đúng Tiền giờ + F&B, đổi trạng thái bàn sang Trống', 'ĐẠT'),
        ('TC07', 'Kiểm tra phân quyền', 'Tài khoản staff01 truy cập Quản lý NV', 'Hệ thống chặn truy cập và thông báo quyền bị từ chối', 'ĐẠT')
    ]
    for row_idx, r_data in enumerate(tc_data):
        for col_idx, text in enumerate(r_data):
            c = t_tc.rows[row_idx].cells[col_idx]
            c.paragraphs[0].text = text
            if row_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True
            c.paragraphs[0].paragraph_format.line_spacing = 1.5
            for run in c.paragraphs[0].runs:
                run.font.name = 'Times New Roman'
                run.font.size = Pt(11)
                run.font.color.rgb = RGBColor(0, 0, 0)
            set_cell_border(c, top=border_spec, bottom=border_spec, left=border_spec, right=border_spec)

    add_heading_3('3.3.2. Kiểm thử bảo mật phân quyền RBAC và kiểm thử độ bền kết nối mạng')
    add_para('Thử nghiệm đăng nhập với tài khoản nhân viên thu ngân `staff01` cho thấy: nhân viên có thể mở bàn, gọi món và tính tiền bình thường. Tuy nhiên, khi nhân viên cố gắng chuyển menu sang phân hệ Quản trị nhân viên hoặc Cấu hình quyền, hệ thống kích hoạt cơ chế `UserSession.DemandPermission`, lập tức ngăn chặn hành vi trái phép và ghi lại cảnh báo vào bảng `AuditLogs`.')

    add_heading_3('3.3.3. Đánh giá hiệu năng và phản hồi thực tế của người dùng')
    add_para('Nhóm nghiên cứu đã thực hiện đo lường thời gian thực thi của các thao tác dữ liệu qua kênh truyền Internet kết nối máy chủ Aiven Cloud SSL. Kết quả đo lường trung bình được thể hiện trong biểu đồ hình 3.2.')

    add_image_safe('docs/figures/fig_3_2_performance_chart.png')
    add_fig_title('Hình 3.2: Biểu đồ kết quả kiểm thử tải và thời gian đáp ứng cơ sở dữ liệu đám mây')

    doc.add_page_break()

    # ==========================
    # 9. KẾT LUẬN
    # ==========================
    add_heading_1('KẾT LUẬN')
    
    add_heading_2('4.1. Tóm tắt kết quả nghiên cứu và sản phẩm đạt được')
    add_para('Sau quá trình nghiên cứu lý thuyết và trực tiếp triển khai mã nguồn, nhóm sinh viên đã hoàn thành đề tài "Xây dựng hệ thống phần mềm quản lý quán Billiard (Billiard Pro) trên nền tảng C# .NET WPF và cơ sở dữ liệu MySQL". Các kết quả chính đạt được bao gồm:')
    add_para('• Xây dựng thành công cơ sở dữ liệu hoàn chỉnh gồm 17 bảng đạt chuẩn dạng chuẩn 3 (3NF), giải quyết xuất sắc bài toán tính tiền giờ lũy tiến và chia khoảng thời gian khi chuyển bàn.')
    add_para('• Ứng dụng thành công mô hình kiến trúc chuẩn MVVM trên nền tảng .NET Framework 4.7.2 và công nghệ WPF XAML, mang lại giao diện người dùng trực quan, thẩm mỹ cao và vận hành ổn định.')
    add_para('• Hiện thực hóa cơ chế bảo mật đa lớp: mã hóa mật khẩu một chiều BCrypt kèm muối ngẫu nhiên, phân quyền theo vai trò (RBAC) 8 quyền hạn độc lập, bảo mật chuỗi kết nối bằng file `connections.config` và lưu vết kiểm toán toàn diện bằng bảng `AuditLogs`.')

    add_heading_2('4.2. So sánh đối chiếu với mục tiêu đề ra ban đầu')
    add_para('Đối chiếu với đề cương yêu cầu của học phần Công nghệ .NET, sản phẩm đã hoàn thành 100% các mục tiêu đề ra ban đầu:')
    add_para('• Đáp ứng đầy đủ các nghiệp vụ thực tế của một câu lạc bộ Billiard hiện đại (quản lý bàn thời gian thực, mở phiên, chuyển bàn, gọi dịch vụ F&B, thanh toán và in hóa đơn).')
    add_para('• Hệ thống đã được kiểm thử chức năng và đo lường hiệu năng thực tế, tất cả 7 ca kiểm thử trọng yếu đều đạt kết quả tốt với thời gian phản hồi dưới 150 miligiây.')

    add_heading_2('4.3. Đánh giá ưu điểm, hạn chế và hướng phát triển tương lai')
    add_para('• Ưu điểm: Kiến trúc phần mềm tách bạch, dễ mở rộng và bảo trì; dữ liệu lưu trữ đám mây an toàn; giải quyết trọn vẹn bài toán chống thất thoát tài chính cho chủ quán.')
    add_para('• Hạn chế: Phần mềm hiện tại mới chỉ hỗ trợ nền tảng Desktop Windows; chưa tích hợp trực tiếp phần cứng điều khiển rơ-le ngắt nguồn điện bóng đèn từng bàn khi đóng/mở phiên.')
    add_para('• Hướng phát triển trong tương lai:')
    add_para('  - Tích hợp mạch điều khiển phần cứng IoT (ESP32 / Arduino) qua giao thức MQTT để tự động bật/tắt điện đèn bàn tương ứng khi mở bàn hoặc thanh toán hóa đơn.')
    add_para('  - Phát triển ứng dụng di động (Mobile App) trên nền tảng .NET MAUI dành riêng cho chủ quán để theo dõi báo cáo doanh thu từ xa theo thời gian thực.')
    add_para('  - Mở rộng chức năng tích điểm thành viên, hội viên VIP và phát hành voucher khuyến mãi tự động qua mã QR.')

    doc.add_page_break()

    # ==========================
    # 10. DANH MỤC TÀI LIỆU THAM KHẢO (IEEE Standard)
    # ==========================
    add_heading_1('DANH MỤC TÀI LIỆU THAM KHẢO')
    refs = [
        '[1] N. V. Hùng và T. T. Long, "Nghiên cứu mô hình quản lý vận hành dịch vụ thể thao giải trí tại Việt Nam," Tạp chí Khoa học & Đào tạo Thể thao, Tập 14, Số 2, tr. 45–53, 2021.',
        '[2] P. N. Hưng, Giáo trình Công nghệ .NET và Lập trình Hướng đối tượng nâng cao, Hà Nội: Nxb Thông tin và Truyền thông, 2022.',
        '[3] A. Nathan, WPF 4.5 Unleashed, 1st ed. Indianapolis, IN, USA: Sams Publishing, 2013.',
        '[4] J. Liberty and D. Hurwitz, Programming .NET Windows Applications, 2nd ed. Sebastopol, CA, USA: O\'Reilly Media, 2018.',
        '[5] B. Albahari and J. Albahari, C# 10 in a Nutshell: The Definitive Reference, 1st ed. Sebastopol, CA, USA: O\'Reilly Media, 2022.',
        '[6] MySQL AB, "MySQL 8.0 Reference Manual: Security and InnoDB Architecture," 2023. [Trực tuyến]. Địa chỉ: https://dev.mysql.com/doc/refman/8.0/en/ [Truy cập ngày 15/10/2026].',
        '[7] Microsoft Learn, "Model-View-ViewModel (MVVM) Design Pattern in WPF Applications," 2024. [Trực tuyến]. Địa chỉ: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/ [Truy cập ngày 20/10/2026].'
    ]
    for r in refs:
        add_para(r, space_after=6)

    doc.add_page_break()

    # ==========================
    # 11. PHỤ LỤC
    # ==========================
    add_heading_1('PHỤ LỤC')
    
    add_heading_2('Phụ lục 1: Kịch bản kiểm thử tích hợp chi tiết (Integration Test Script)')
    add_para('Dưới đây là kịch bản chạy thử nghiệm chu trình hoàn chỉnh một phiên chơi thực tế tại quán Billiard:')
    add_para('1. Bước 1: Nhân viên `staff01` đăng nhập vào phần mềm Billiard Pro với mật khẩu `Admin@123`.')
    add_para('2. Bước 2: Quan sát sơ đồ bàn, nhân viên chọn bàn Pool số `P02` (đang có trạng thái Available - Trống) và bấm nút "Mở bàn". Hệ thống kích hoạt đồng hồ đếm giờ và đổi màu bàn.')
    add_para('3. Bước 3: Sau 20 phút chơi, khách gọi 2 lon bò húc (Red Bull) và 1 bao thuốc lá 555. Nhân viên chọn bàn `P02`, chọn mục "Dịch vụ & F&B", thêm các món tương ứng vào bàn.')
    add_para('4. Bước 4: Sau 45 phút chơi, khách có thêm bạn đến và muốn chuyển sang bàn Carom 3 Băng `3B01`. Nhân viên bấm chọn "Chuyển bàn", chọn bàn đích `3B01`. Hệ thống tự động chốt số tiền của 45 phút bàn Pool và chuyển phiên sang bàn 3 Băng.')
    add_para('5. Bước 5: Khi khách yêu cầu tính tiền, nhân viên bấm nút "Thanh toán". Màn hình thanh toán xuất hiện tổng kết chi tiết: Tiền giờ bàn Pool + Tiền giờ bàn 3 Băng + Tiền dịch vụ F&B. Nhân viên chọn hình thức Tiền mặt và bấm "Hoàn tất". Hệ thống in hóa đơn và chuyển bàn về trạng thái Trống.')

    add_heading_2('Phụ lục 2: Mẫu phiếu in hóa đơn thanh toán (Invoice Receipt Sample)')
    add_para('------------------------------------------------------------', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('CLB BILLIARD PRO PHENIKAA', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True)
    add_para('Địa chỉ: Yên Nghĩa, Hà Đông, Hà Nội - Hotline: 0987.654.321', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('PHIẾU THANH TOÁN DỊCH VỤ', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True)
    add_para('Số HĐ: HD261104-001             Ngày: 04/11/2026 21:30:15', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('Thu ngân: staff01                Khách hàng: Khách lẻ', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('------------------------------------------------------------', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('Bàn P02 (Pool)     [19:00 - 19:45] (45 phút)   :  37.500,00 VNĐ')
    add_para('Bàn 3B01 (3 Băng)  [19:45 - 21:30] (105 phút)  : 140.000,00 VNĐ')
    add_para('Nước tăng lực Red Bull (2 Lon x 20.000,00)     :  40.000,00 VNĐ')
    add_para('Thuốc lá 555 Ngoại (1 Gói x 35.000,00)         :  35.000,00 VNĐ')
    add_para('------------------------------------------------------------', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('Tổng tiền giờ bàn                               : 177.500,00 VNĐ')
    add_para('Tổng tiền dịch vụ F&B                           :  75.000,00 VNĐ')
    add_para('Chiết khấu / Giảm giá                           :       0,00 VNĐ')
    add_para('TỔNG CỘNG THANH TOÁN                            : 252.500,00 VNĐ', bold=True)
    add_para('Hình thức: Tiền mặt (Cash)', italic=True)
    add_para('------------------------------------------------------------', align=WD_ALIGN_PARAGRAPH.CENTER)
    add_para('CẢM ƠN QUÝ KHÁCH VÀ HẸN GẶP LẠI!', align=WD_ALIGN_PARAGRAPH.CENTER, bold=True)

    # Save to file
    out_path = 'docs/Bao_Cao_BTL_Billiard_Net_Phenikaa.docx'
    doc.save(out_path)
    print(f'Document successfully created and saved at {out_path}')

if __name__ == '__main__':
    create_report_docx()
