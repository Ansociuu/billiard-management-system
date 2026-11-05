# -*- coding: utf-8 -*-
"""Đợt 3: đặc tả lớp (trích từ mã nguồn thành bảng) và user story."""
import os, re
from extra_content import para, h2, h3, bullets, table, SRC

PROP = re.compile(r'^\s*public\s+(?!class|static|async|override|abstract|event|const)([\w<>\[\]?,\.]+)\s+(\w+)\s*(\{|=>|$)')
CMD = re.compile(r'^\s*public\s+(?:I?\w*Command)\s+(\w+)')
SUM = re.compile(r'///\s*(?:<summary>)?\s*(.*?)\s*(?:</summary>)?\s*$')


def props(path):
    out, last_doc = [], ''
    with open(path, encoding='utf-8-sig', errors='replace') as f:
        for line in f:
            m = SUM.match(line.strip()) if line.strip().startswith('///') else None
            if m and m.group(1) and 'summary' not in m.group(1):
                last_doc = m.group(1)
            p = PROP.match(line)
            if p and '(' not in line.split('{')[0]:
                out.append((p.group(2), p.group(1), last_doc))
                last_doc = ''
    return out


TYPE_VN = {'string': 'Chuỗi', 'int': 'Số nguyên', 'long': 'Số nguyên lớn', 'decimal': 'Số thập phân (tiền)',
           'bool': 'Đúng/Sai', 'DateTime': 'Ngày giờ', 'TimeSpan': 'Khoảng thời gian'}


def vn(t):
    base = t.replace('?', '')
    s = TYPE_VN.get(base, t)
    return s + (' (cho phép rỗng)' if t.endswith('?') else '')


MODEL_DESC = {
    'User': 'tài khoản đăng nhập hệ thống', 'Role': 'vai trò người dùng', 'Permission': 'quyền hạn của hệ thống',
    'BilliardTable': 'bàn chơi Billiard và trạng thái thời gian thực', 'TableType': 'loại bàn (Pool, Carom, Snooker...)',
    'TablePricingRule': 'quy tắc định giá theo khung giờ, loại ngày và khuyến mại', 'BilliardSession': 'phiên chơi của một bàn',
    'SessionProduct': 'sản phẩm dịch vụ gọi trong một phiên chơi', 'Product': 'sản phẩm dịch vụ bán kèm',
    'ProductCategory': 'danh mục sản phẩm', 'Customer': 'khách hàng', 'Employee': 'hồ sơ nhân viên',
    'Invoice': 'hóa đơn thanh toán', 'InvoiceDetail': 'dòng chi tiết của hóa đơn'}


def c2(doc):
    h2(doc, '2.17. Đặc tả chi tiết các lớp thực thể (Model)')
    para(doc, 'Các lớp thuộc thư mục Models ánh xạ trực tiếp tới các bảng của cơ sở dữ liệu và kế thừa ObservableObject để hỗ trợ liên kết dữ liệu. Phần này liệt kê các thuộc tính của từng lớp, giúp người đọc đối chiếu giữa mô hình đối tượng trong ứng dụng và cấu trúc bảng dữ liệu.')
    mdir = os.path.join(SRC, 'Models')
    n = 0
    for name in ['User', 'Role', 'Permission', 'TableType', 'BilliardTable', 'TablePricingRule', 'BilliardSession',
                 'SessionProduct', 'ProductCategory', 'Product', 'Customer', 'Employee', 'Invoice', 'InvoiceDetail']:
        path = os.path.join(mdir, name + '.cs')
        if not os.path.exists(path):
            continue
        ps = props(path)
        if not ps:
            continue
        n += 1
        h3(doc, '2.17.%d. Lớp %s' % (n, name))
        para(doc, 'Lớp %s biểu diễn %s. Lớp gồm %d thuộc tính công khai như trong bảng dưới đây.' % (name, MODEL_DESC.get(name, 'một thực thể của hệ thống'), len(ps)))
        rows = []
        for pn, pt, d in ps:
            rows.append([pn, vn(pt), d if d else ('Thuộc tính %s của %s' % (pn, name))])
        table(doc, 'Bảng 2.17.%d. Các thuộc tính của lớp %s' % (n, name), ['Thuộc tính', 'Kiểu dữ liệu', 'Ý nghĩa'], rows,
              widths=[4.4, 4.0, 5.8], size=11)
    h2(doc, '2.18. Đặc tả chi tiết các lớp ViewModel')
    para(doc, 'Các ViewModel là nơi tập trung logic trình bày của từng màn hình. Mỗi ViewModel cung cấp các thuộc tính để giao diện liên kết dữ liệu và các lệnh để giao diện kích hoạt hành động. Bảng dưới đây liệt kê các thuộc tính và lệnh công khai của từng ViewModel.')
    vdir = os.path.join(SRC, 'ViewModels')
    desc = {'LoginViewModel': 'màn hình đăng nhập', 'MainViewModel': 'cửa sổ chính, điều hướng và thông báo',
            'TableManagementViewModel': 'màn hình sơ đồ bàn, phiên chơi và quy tắc giá',
            'ProductManagementViewModel': 'màn hình quản lý sản phẩm và danh mục',
            'StaffManagementViewModel': 'màn hình quản lý nhân viên và phân quyền',
            'BaseViewModel': 'lớp cơ sở cho các ViewModel', 'MenuPlaceholderViewModel': 'màn hình tạm cho chức năng chưa hoàn thiện'}
    k = 0
    for name in ['BaseViewModel', 'LoginViewModel', 'MainViewModel', 'TableManagementViewModel', 'ProductManagementViewModel', 'StaffManagementViewModel']:
        path = os.path.join(vdir, name + '.cs')
        if not os.path.exists(path):
            continue
        ps = props(path)
        cmds = []
        with open(path, encoding='utf-8-sig', errors='replace') as f:
            for line in f:
                m = CMD.match(line)
                if m:
                    cmds.append(m.group(1))
        k += 1
        h3(doc, '2.18.%d. %s' % (k, name))
        para(doc, '%s phụ trách %s. ' % (name, desc.get(name, 'một màn hình của ứng dụng')) +
             'Lớp có %d thuộc tính công khai và %d lệnh công khai.' % (len(ps), len(cmds)))
        if ps:
            rows = [[pn, vn(pt)] for pn, pt, d in ps[:40]]
            table(doc, 'Bảng 2.18.%da. Thuộc tính của %s' % (k, name), ['Thuộc tính', 'Kiểu dữ liệu'], rows, widths=[7.0, 7.2], size=11)
        if cmds:
            table(doc, 'Bảng 2.18.%db. Các lệnh (Command) của %s' % (k, name), ['Tên lệnh'], [[c] for c in cmds], widths=[14.2], size=11)
    h2(doc, '2.19. Câu chuyện người dùng (User Story)')
    para(doc, 'Để bảo đảm thiết kế bám sát nhu cầu thực tế, các yêu cầu được diễn đạt lại dưới dạng câu chuyện người dùng theo mẫu: "Với vai trò [vai trò], tôi muốn [mục tiêu] để [lợi ích]". Cách diễn đạt này nhấn mạnh giá trị mang lại cho từng người dùng.')
    stories = [
        ['US-01', 'Thu ngân', 'đăng nhập bằng tài khoản riêng', 'mọi thao tác của tôi được ghi nhận đúng người'],
        ['US-02', 'Thu ngân', 'nhìn sơ đồ bàn với màu sắc trạng thái', 'biết ngay bàn nào còn trống'],
        ['US-03', 'Thu ngân', 'mở bàn chỉ bằng vài thao tác', 'không để khách phải chờ'],
        ['US-04', 'Thu ngân', 'xem thời gian chơi chạy tự động', 'không phải nhớ hay tính nhẩm giờ vào'],
        ['US-05', 'Thu ngân', 'thêm món vào bàn đang chơi', 'không bỏ sót món khi thanh toán'],
        ['US-06', 'Thu ngân', 'chuyển bàn mà không mất dữ liệu', 'phục vụ khách muốn đổi chỗ'],
        ['US-07', 'Thu ngân', 'xem tổng tiền chi tiết trước khi thu', 'giải thích rõ ràng cho khách'],
        ['US-08', 'Chủ quán', 'cấu hình giá theo khung giờ và ngày', 'quán áp dụng chính sách giá linh hoạt'],
        ['US-09', 'Chủ quán', 'tạo chương trình khuyến mại có thời hạn', 'thu hút khách vào giờ thấp điểm'],
        ['US-10', 'Chủ quán', 'quản lý thực đơn và giá bán', 'cập nhật nhanh khi có món mới'],
        ['US-11', 'Chủ quán', 'thêm, khóa tài khoản nhân viên', 'kiểm soát người được truy cập hệ thống'],
        ['US-12', 'Chủ quán', 'phân quyền khác nhau cho từng vai trò', 'nhân viên không làm ngoài phạm vi'],
        ['US-13', 'Chủ quán', 'xem nhật ký thao tác', 'truy vết khi có chênh lệch'],
        ['US-14', 'Chủ quán', 'dữ liệu được lưu trên đám mây', 'không mất dữ liệu khi máy tại quán hỏng'],
        ['US-15', 'Quản trị hệ thống', 'tách cấu hình kết nối khỏi mã nguồn', 'không lộ mật khẩu khi chia sẻ mã']]
    table(doc, 'Bảng 2.19.1. Danh sách câu chuyện người dùng',
          ['Mã', 'Với vai trò', 'Tôi muốn', 'Để'], stories, widths=[1.6, 2.6, 5.4, 4.6], size=11)
    h2(doc, '2.20. Tổng kết chương')
    para(doc, 'Chương 2 đã phân tích yêu cầu, lựa chọn kiến trúc MVVM và giải pháp cơ sở dữ liệu đám mây, thiết kế chức năng thông qua use case và các sơ đồ tuần tự, thiết kế cơ sở dữ liệu 17 bảng, thiết kế giao diện, các quy tắc kiểm tra dữ liệu và các biện pháp bảo mật. Phần đặc tả lớp và câu chuyện người dùng cung cấp cầu nối trực tiếp giữa thiết kế và mã nguồn triển khai, làm cơ sở cho việc hiện thực ở chương 3.')


def c1(doc):
    h2(doc, '1.13. Tổng kết chương')
    para(doc, 'Chương 1 đã trình bày bối cảnh và đặc thù nghiệp vụ của quán Billiard, phân tích hiện trạng và những bất cập của cách quản lý thủ công, khảo sát các giải pháp hiện có, xác định mục tiêu, phạm vi, tiêu chí nghiệm thu cùng danh sách yêu cầu chức năng và phi chức năng chi tiết. Đây là nền tảng để chương 2 tiến hành phân tích và thiết kế hệ thống.')
