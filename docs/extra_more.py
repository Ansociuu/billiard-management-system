# -*- coding: utf-8 -*-
"""Nội dung văn bản bổ sung cho chương 1, 2, 3 (không chứa mã nguồn)."""
from extra_content import para, h2, h3, bullets, table


def uc(doc, code, name, actor, pre, post, main, alt):
    table(doc, 'Bảng 2.10.%s. Đặc tả use case %s: %s' % (code[-2:].lstrip('0'), code, name),
          ['Mục', 'Nội dung'],
          [['Mã use case', code], ['Tên', name], ['Tác nhân', actor],
           ['Điều kiện trước', pre], ['Điều kiện sau', post],
           ['Luồng chính', main], ['Luồng ngoại lệ', alt]],
          widths=[3.2, 11.0], size=11.5)


def c1(doc):
    h2(doc, '1.7. Khảo sát đặc thù nghiệp vụ của quán Billiard')
    h3(doc, '1.7.1. Các loại bàn và cách tính giờ')
    para(doc, 'Một quán Billiard thường kinh doanh nhiều loại bàn khác nhau, mỗi loại có kích thước, thiết bị và đối tượng khách riêng. Bàn Pool (còn gọi là bàn lỗ) là loại phổ biến nhất ở các quán bình dân, phù hợp với nhóm bạn trẻ và có mức giá giờ thấp. Bàn Carom (không lỗ) gồm các thể loại Libre, 3 băng, đòi hỏi kỹ thuật cao và thường có giá giờ cao hơn do bàn lớn, bi và nỉ chuyên dụng. Bàn Snooker có kích thước lớn nhất, chiếm nhiều diện tích và có giá giờ cao nhất, nên thường chỉ có ở những câu lạc bộ có quy mô lớn hơn.')
    para(doc, 'Cách tính tiền phổ biến nhất là tính theo thời gian sử dụng bàn. Tuy nhiên, chi tiết lại khác nhau giữa các quán: có quán làm tròn theo từng 15 phút, có quán tính chính xác theo phút, có quán quy định mức tối thiểu 30 phút cho mỗi lượt chơi. Ngoài ra, giá giờ còn thay đổi theo khung giờ trong ngày (giờ thấp điểm buổi sáng và chiều, giờ cao điểm buổi tối), theo loại ngày (ngày thường, cuối tuần, ngày lễ) và theo các chương trình khuyến mại tạm thời. Sự phức tạp này chính là lý do một bảng giá niêm yết đơn giản không còn đủ, và cần một cơ chế quy tắc giá linh hoạt như hệ thống đề xuất.')
    h3(doc, '1.7.2. Dịch vụ đi kèm và tổ chức ca làm việc')
    para(doc, 'Doanh thu của quán không chỉ đến từ tiền giờ bàn mà còn từ dịch vụ ăn uống đi kèm: đồ uống đóng chai, nước ngọt, bia, cà phê, đồ ăn nhẹ, thuốc lá và các phụ kiện tiêu hao như phấn, găng tay. Phần doanh thu này thường chiếm tỷ trọng đáng kể và có biên lợi nhuận cao, song lại là nơi thất thoát nhiều nhất vì việc ghi nhận bằng tay dễ sót. Một quán thường chia làm hai đến ba ca (sáng, chiều, tối), mỗi ca có một hoặc hai nhân viên phụ trách thu ngân và phục vụ. Cuối mỗi ca, nhân viên phải bàn giao số tiền thu được và đối chiếu với số liệu ghi chép; việc này mất thời gian và dễ phát sinh tranh cãi khi không có dữ liệu truy vết.')
    h3(doc, '1.7.3. Đặc điểm của khách hàng')
    para(doc, 'Khách hàng của quán gồm khách vãng lai chơi một lần và khách quen chơi thường xuyên. Khách quen thường mong muốn được ghi nhận, hưởng ưu đãi hoặc tích lũy điểm. Khách chơi theo nhóm thường đổi bàn, gộp bàn hoặc kéo dài thời gian, đòi hỏi hệ thống phải xử lý linh hoạt các thay đổi này mà vẫn tính tiền đúng. Một đặc điểm khác là khách thường muốn xem lại chi tiết hóa đơn để đối chiếu; vì vậy hóa đơn cần minh bạch, thể hiện rõ từng khoản tiền giờ và từng món dịch vụ.')
    h2(doc, '1.8. Khảo sát các giải pháp hiện có và lý do xây dựng hệ thống mới')
    h3(doc, '1.8.1. Các hình thức quản lý đang được sử dụng')
    para(doc, 'Qua khảo sát, nhóm nhận thấy các quán hiện nay quản lý theo bốn hình thức chính: ghi chép bằng sổ tay, sử dụng bảng tính Excel, dùng phần mềm bán hàng tổng quát (POS) và dùng phần mềm chuyên dụng cho quán Billiard. Mỗi hình thức có ưu và nhược điểm riêng, được tổng hợp ở bảng dưới đây.')
    table(doc, 'Bảng 1.8.1. So sánh các hình thức quản lý quán Billiard',
          ['Hình thức', 'Ưu điểm', 'Nhược điểm'],
          [['Sổ tay, bảng viết tay', 'Không tốn chi phí, dễ làm quen', 'Sai sót giờ, tính nhẩm, không truy vết, khó thống kê, dễ thất thoát'],
           ['Bảng tính Excel', 'Linh hoạt, chi phí thấp, có công thức', 'Không có đồng hồ thời gian thực, nhập tay, khó dùng nhiều máy, không phân quyền'],
           ['Phần mềm POS tổng quát', 'Quản lý hàng hóa và hóa đơn tốt', 'Không có khái niệm bàn tính giờ, không hỗ trợ giá theo khung giờ'],
           ['Phần mềm chuyên dụng sẵn có', 'Đủ chức năng, có hỗ trợ kỹ thuật', 'Chi phí bản quyền hoặc thuê bao hằng tháng, khó tùy biến, dữ liệu phụ thuộc nhà cung cấp'],
           ['Hệ thống đề xuất (Billiard Pro)', 'Đúng nghiệp vụ, tùy biến theo nhu cầu, dữ liệu trên đám mây, chi phí thấp', 'Cần Internet, phiên bản đầu còn ở phạm vi cơ bản']],
          widths=[3.6, 5.0, 5.6], size=11.5)
    h3(doc, '1.8.2. Lý do chọn xây dựng hệ thống mới')
    para(doc, 'Việc xây dựng hệ thống mới có ba lý do chính. Một là, mục tiêu của đề tài là học tập và vận dụng công nghệ .NET vào một bài toán thực tế có đủ các thành phần của một ứng dụng quản lý: giao diện, nghiệp vụ, cơ sở dữ liệu, phân quyền và bảo mật. Hai là, bài toán quản lý quán Billiard có nghiệp vụ đặc thù (tính giờ theo khung, theo loại bàn) chưa được các phần mềm tổng quát đáp ứng gọn. Ba là, việc tự xây dựng cho phép nhóm hiểu và kiểm soát toàn bộ kiến trúc, từ đó dễ mở rộng thêm các chức năng ở các giai đoạn sau.')
    h2(doc, '1.9. Mục tiêu, phạm vi và tiêu chí nghiệm thu')
    h3(doc, '1.9.1. Mục tiêu của đề tài')
    bullets(doc, [
        'Xây dựng ứng dụng desktop quản lý quán Billiard trên nền tảng C# .NET Framework và WPF, theo kiến trúc MVVM.',
        'Thiết kế và triển khai cơ sở dữ liệu quan hệ MySQL gồm 17 bảng, đặt trên dịch vụ đám mây Aiven.',
        'Hiện thực các chức năng cốt lõi: đăng nhập, phân quyền, quản lý bàn, quản lý sản phẩm dịch vụ, quản lý quy tắc giá, quản lý nhân viên và nhật ký hệ thống.',
        'Bảo đảm an toàn: băm mật khẩu BCrypt, kênh truyền SSL, tách riêng chuỗi kết nối.',
        'Kiểm thử và đánh giá hệ thống, viết báo cáo theo hướng dẫn trình bày của học phần.'])
    h3(doc, '1.9.2. Phạm vi và giới hạn')
    para(doc, 'Hệ thống tập trung vào bài toán vận hành tại một quán, một cơ sở. Phiên bản hiện tại chưa hỗ trợ đặt bàn trực tuyến cho khách, chưa tích hợp thiết bị phần cứng như máy in nhiệt hoặc đèn điều khiển bàn, chưa hỗ trợ ứng dụng di động. Các hạn chế này được ghi nhận ở phần hướng phát triển. Hệ thống cần kết nối Internet để truy cập cơ sở dữ liệu đám mây nên chưa hoạt động ở chế độ ngoại tuyến hoàn toàn.')
    h3(doc, '1.9.3. Tiêu chí nghiệm thu')
    table(doc, 'Bảng 1.9.1. Tiêu chí nghiệm thu của đề tài',
          ['Tiêu chí', 'Mô tả', 'Cách kiểm tra'],
          [['Đúng chức năng', 'Các chức năng trong danh sách yêu cầu hoạt động đúng', 'Chạy bộ ca kiểm thử chức năng'],
           ['Đúng tiền', 'Tiền giờ và tiền dịch vụ tính đúng theo quy tắc giá', 'So sánh kết quả với tính tay'],
           ['An toàn', 'Mật khẩu không lưu rõ, chuỗi kết nối không lộ, người dùng không vượt quyền', 'Kiểm thử bảo mật và rà soát mã nguồn'],
           ['Ổn định', 'Ứng dụng không treo khi mạng chậm hoặc mất kết nối', 'Kiểm thử độ bền kết nối'],
           ['Dễ dùng', 'Thu ngân mới có thể thao tác cơ bản sau thời gian hướng dẫn ngắn', 'Quan sát và thu thập phản hồi']],
          widths=[3.0, 6.4, 4.8], size=11.5)
    h2(doc, '1.10. Danh sách yêu cầu chức năng chi tiết')
    para(doc, 'Phần này liệt kê chi tiết các yêu cầu chức năng theo từng phân hệ, mỗi yêu cầu có mã, mô tả và mức độ ưu tiên (Cao, Trung bình, Thấp). Mức Cao là các chức năng bắt buộc phải có để hệ thống vận hành được.')
    rows = [
        ['FR-01.1', 'Xác thực', 'Đăng nhập bằng tên đăng nhập và mật khẩu', 'Cao'],
        ['FR-01.2', 'Xác thực', 'Ghi nhớ tên đăng nhập trên máy', 'Thấp'],
        ['FR-01.3', 'Xác thực', 'Đăng xuất và xóa phiên làm việc', 'Cao'],
        ['FR-01.4', 'Xác thực', 'Từ chối tài khoản bị khóa', 'Cao'],
        ['FR-02.1', 'Phân quyền', 'Gán quyền cho vai trò', 'Cao'],
        ['FR-02.2', 'Phân quyền', 'Ẩn hoặc vô hiệu hóa chức năng khi thiếu quyền', 'Cao'],
        ['FR-02.3', 'Phân quyền', 'Chặn thao tác thiếu quyền tại tầng nghiệp vụ', 'Cao'],
        ['FR-03.1', 'Bàn', 'Thêm, sửa, ngừng hoạt động bàn', 'Cao'],
        ['FR-03.2', 'Bàn', 'Quản lý loại bàn', 'Cao'],
        ['FR-03.3', 'Bàn', 'Hiển thị sơ đồ bàn theo trạng thái bằng màu sắc', 'Cao'],
        ['FR-03.4', 'Bàn', 'Lọc bàn theo loại hoặc trạng thái', 'Trung bình'],
        ['FR-04.1', 'Phiên chơi', 'Mở bàn và bắt đầu tính giờ', 'Cao'],
        ['FR-04.2', 'Phiên chơi', 'Hiển thị thời gian đã chơi theo thời gian thực', 'Cao'],
        ['FR-04.3', 'Phiên chơi', 'Chuyển bàn giữ nguyên phiên chơi', 'Trung bình'],
        ['FR-04.4', 'Phiên chơi', 'Thanh toán và kết thúc phiên', 'Cao'],
        ['FR-05.1', 'Dịch vụ', 'Quản lý danh mục sản phẩm', 'Cao'],
        ['FR-05.2', 'Dịch vụ', 'Quản lý sản phẩm, giá bán, hình ảnh', 'Cao'],
        ['FR-05.3', 'Dịch vụ', 'Thêm sản phẩm vào phiên chơi', 'Cao'],
        ['FR-06.1', 'Giá', 'Cấu hình giá nền theo loại bàn', 'Cao'],
        ['FR-06.2', 'Giá', 'Cấu hình giá theo khung giờ và loại ngày', 'Cao'],
        ['FR-06.3', 'Giá', 'Cấu hình khuyến mại theo thời gian hiệu lực', 'Trung bình'],
        ['FR-07.1', 'Nhân sự', 'Thêm, sửa, khóa tài khoản nhân viên', 'Cao'],
        ['FR-07.2', 'Nhân sự', 'Đặt lại mật khẩu', 'Trung bình'],
        ['FR-08.1', 'Nhật ký', 'Ghi nhật ký các thao tác quan trọng', 'Cao'],
    ]
    table(doc, 'Bảng 1.10.1. Danh sách yêu cầu chức năng chi tiết',
          ['Mã', 'Phân hệ', 'Mô tả yêu cầu', 'Ưu tiên'], rows,
          widths=[1.9, 2.4, 7.9, 2.0], size=11.5)
    h3(doc, '1.10.1. Yêu cầu phi chức năng chi tiết')
    table(doc, 'Bảng 1.10.2. Yêu cầu phi chức năng',
          ['Mã', 'Nhóm', 'Mô tả', 'Chỉ tiêu'],
          [['NFR-01', 'Hiệu năng', 'Thời gian phản hồi thao tác thông thường', 'Dưới 2 giây trong điều kiện mạng bình thường'],
           ['NFR-02', 'Hiệu năng', 'Giao diện không bị đóng băng khi truy vấn', 'Mọi truy vấn chạy bất đồng bộ'],
           ['NFR-03', 'Bảo mật', 'Lưu trữ mật khẩu', 'Băm một chiều BCrypt, không lưu rõ'],
           ['NFR-04', 'Bảo mật', 'Kênh truyền tới CSDL', 'SSL/TLS bắt buộc'],
           ['NFR-05', 'Bảo mật', 'Bảo vệ thông tin cấu hình', 'Không đưa chuỗi kết nối thật lên kho mã'],
           ['NFR-06', 'Tin cậy', 'Toàn vẹn dữ liệu', 'Dùng khóa ngoại và giao dịch'],
           ['NFR-07', 'Khả dụng', 'Giao diện tiếng Việt, trực quan', 'Hiển thị đúng ký tự có dấu'],
           ['NFR-08', 'Bảo trì', 'Mã nguồn tách tầng', 'Kiến trúc MVVM, dịch vụ tách riêng'],
           ['NFR-09', 'Tương thích', 'Hệ điều hành', 'Windows 10 và 11']],
          widths=[1.9, 2.3, 5.2, 4.8], size=11.5)


def c2(doc):
    h2(doc, '2.10. Đặc tả chi tiết các use case')
    para(doc, 'Mỗi use case dưới đây được đặc tả theo các mục: tác nhân, điều kiện trước, điều kiện sau, luồng chính và luồng ngoại lệ. Cách đặc tả này giúp làm rõ hành vi mong đợi của hệ thống trước khi lập trình và là cơ sở để xây dựng ca kiểm thử.')
    uc(doc, 'UC01', 'Đăng nhập hệ thống', 'Quản trị viên, Thu ngân',
       'Ứng dụng đã khởi động, có kết nối tới cơ sở dữ liệu.',
       'Phiên làm việc được tạo, danh sách quyền được nạp, màn hình chính được hiển thị.',
       '1) Người dùng nhập tên đăng nhập và mật khẩu. 2) Nhấn Đăng nhập. 3) Hệ thống kiểm tra dữ liệu nhập. 4) Hệ thống truy vấn tài khoản. 5) So khớp mật khẩu với giá trị băm BCrypt. 6) Nạp quyền theo vai trò. 7) Ghi nhật ký đăng nhập. 8) Mở màn hình chính.',
       'Bỏ trống thông tin: báo lỗi nhập thiếu. Sai tài khoản hoặc mật khẩu: báo sai thông tin. Tài khoản bị khóa: từ chối đăng nhập. Mất kết nối: thông báo lỗi kết nối và cho phép thử lại.')
    uc(doc, 'UC02', 'Quản lý bàn chơi', 'Quản trị viên',
       'Đã đăng nhập và có quyền quản lý bàn.',
       'Danh sách bàn được cập nhật trong cơ sở dữ liệu và trên sơ đồ.',
       '1) Chọn chức năng quản lý bàn. 2) Chọn thêm mới hoặc chọn một bàn để sửa. 3) Nhập số bàn, chọn loại bàn, ghi chú. 4) Hệ thống kiểm tra trùng số bàn. 5) Lưu vào cơ sở dữ liệu. 6) Ghi nhật ký. 7) Làm mới sơ đồ bàn.',
       'Trùng số bàn: báo lỗi, không lưu. Thiếu quyền: chặn thao tác. Bàn đang có phiên chơi: không cho ngừng hoạt động.')
    uc(doc, 'UC03', 'Mở bàn và bắt đầu tính giờ', 'Thu ngân',
       'Bàn ở trạng thái Trống; người dùng có quyền thao tác bàn.',
       'Phiên chơi mới được tạo, bàn chuyển sang Đang chơi, bộ đếm bắt đầu chạy.',
       '1) Thu ngân chọn bàn trống trên sơ đồ. 2) Chọn Mở bàn. 3) Hệ thống tạo bản ghi phiên chơi với thời điểm bắt đầu. 4) Cập nhật trạng thái bàn. 5) Hiển thị bộ đếm thời gian trên thẻ bàn.',
       'Bàn đã có phiên mở: từ chối. Bàn đang bảo trì: từ chối. Lỗi ghi dữ liệu: hoàn tác và báo lỗi.')
    uc(doc, 'UC04', 'Thêm dịch vụ vào phiên chơi', 'Thu ngân',
       'Bàn đang có phiên chơi; sản phẩm đang được bán.',
       'Sản phẩm được ghi vào phiên với đơn giá tại thời điểm gọi.',
       '1) Chọn bàn đang chơi. 2) Mở danh sách sản phẩm. 3) Chọn sản phẩm và số lượng. 4) Hệ thống ghi sản phẩm vào phiên với đơn giá hiện hành. 5) Cập nhật tổng tiền dịch vụ.',
       'Sản phẩm đã ngừng bán: không cho chọn. Số lượng không hợp lệ: báo lỗi. Phiên đã đóng: từ chối.')
    uc(doc, 'UC05', 'Chuyển bàn', 'Thu ngân',
       'Bàn nguồn đang chơi, bàn đích đang trống.',
       'Phiên chơi gắn với bàn đích, bàn nguồn trở về Trống.',
       '1) Chọn bàn đang chơi. 2) Chọn Chuyển bàn. 3) Chọn bàn đích. 4) Hệ thống cập nhật phiên và trạng thái hai bàn trong một giao dịch. 5) Làm mới sơ đồ.',
       'Bàn đích không trống: từ chối. Lỗi giữa chừng: hoàn tác toàn bộ giao dịch.')
    uc(doc, 'UC06', 'Thanh toán và lập hóa đơn', 'Thu ngân',
       'Bàn đang có phiên chơi.',
       'Hóa đơn và chi tiết hóa đơn được lưu; phiên kết thúc; bàn trở về Trống.',
       '1) Chọn bàn cần thanh toán. 2) Hệ thống xác định thời điểm kết thúc. 3) Tính tiền giờ theo quy tắc giá. 4) Cộng tiền dịch vụ. 5) Áp dụng giảm giá (nếu có). 6) Hiển thị tổng tiền. 7) Thu ngân xác nhận. 8) Lưu hóa đơn, kết thúc phiên, cập nhật bàn.',
       'Lỗi ghi dữ liệu: hoàn tác giao dịch, bàn vẫn giữ nguyên phiên. Thiếu quy tắc giá: báo cấu hình giá chưa đầy đủ.')
    uc(doc, 'UC07', 'Quản lý quy tắc giá', 'Quản trị viên',
       'Đã đăng nhập và có quyền quản lý giá.',
       'Quy tắc giá được thêm hoặc cập nhật và có hiệu lực theo thời gian khai báo.',
       '1) Chọn chức năng quản lý giá. 2) Chọn loại bàn. 3) Chọn loại quy tắc (giá nền hoặc khuyến mại), loại ngày, khung giờ. 4) Nhập đơn giá hoặc mức giảm, độ ưu tiên, khoảng ngày hiệu lực. 5) Lưu và ghi nhật ký.',
       'Khung giờ không hợp lệ: báo lỗi. Giá âm hoặc bằng 0: báo lỗi. Thiếu quyền: chặn.')
    uc(doc, 'UC08', 'Quản lý nhân viên và phân quyền', 'Quản trị viên',
       'Đã đăng nhập và có quyền quản lý nhân sự.',
       'Tài khoản và quyền được cập nhật.',
       '1) Chọn chức năng nhân viên. 2) Thêm mới hoặc chọn nhân viên cần sửa. 3) Nhập hồ sơ, tên đăng nhập, vai trò. 4) Hệ thống kiểm tra tên đăng nhập duy nhất. 5) Băm mật khẩu BCrypt và lưu. 6) Ghi nhật ký.',
       'Tên đăng nhập trùng: báo lỗi. Mật khẩu quá ngắn: báo lỗi. Admin tự khóa tài khoản của mình: không cho phép.')
    h2(doc, '2.11. Thiết kế chi tiết các thành phần giao diện')
    h3(doc, '2.11.1. Nguyên tắc thiết kế giao diện')
    para(doc, 'Giao diện được thiết kế dựa trên các nguyên tắc: nhất quán (cùng một kiểu nút, màu sắc và bố cục ở mọi màn hình), phản hồi tức thời (mọi thao tác đều có thông báo thành công hoặc lỗi), tối giản thao tác (thu ngân hoàn thành các thao tác thường dùng chỉ với một đến hai lần nhấp) và dễ nhìn trong môi trường quán thường thiếu sáng (dùng màu tương phản rõ). Thư viện Material Design được sử dụng để có bộ điều khiển hiện đại, đồng bộ.')
    h3(doc, '2.11.2. Mô tả các thành phần của màn hình chính')
    table(doc, 'Bảng 2.11.1. Các thành phần của cửa sổ chính (MainWindow)',
          ['Thành phần', 'Chức năng'],
          [['Thanh điều hướng bên trái', 'Danh sách mục chức năng (sơ đồ bàn, sản phẩm, nhân viên, các mục tạm); mục nào người dùng không có quyền sẽ bị ẩn'],
           ['Thanh tiêu đề', 'Hiển thị tên ứng dụng, tên người dùng, vai trò và nút đăng xuất'],
           ['Vùng nội dung', 'Hiển thị View tương ứng với mục đang chọn, được liên kết với ViewModel'],
           ['Vùng thông báo', 'Hiển thị thông báo Info, Success, Warning, Error trong thời gian ngắn rồi tự ẩn']],
          widths=[4.4, 9.8], size=11.5)
    h3(doc, '2.11.3. Mô tả các thành phần của màn hình quản lý bàn')
    para(doc, 'Màn hình quản lý bàn gồm hai khu vực chính. Khu vực thứ nhất là sơ đồ bàn: mỗi bàn được thể hiện bằng một thẻ có số bàn, loại bàn, trạng thái bằng màu sắc, và nếu đang chơi thì hiện thêm bộ đếm thời gian cùng tổng tiền tạm tính. Khu vực thứ hai là bảng điều khiển với các nút thao tác: mở bàn, thêm dịch vụ, chuyển bàn, thanh toán, cùng các công cụ lọc theo loại bàn. Quản trị viên còn có thêm các thẻ quản lý loại bàn và quy tắc giá để cấu hình trực tiếp.')
    h3(doc, '2.11.4. Mô tả các thành phần của màn hình sản phẩm và nhân viên')
    para(doc, 'Màn hình sản phẩm hiển thị danh mục ở bên trái và lưới sản phẩm ở bên phải. Mỗi sản phẩm có tên, giá bán, trạng thái và hình ảnh được lưu trên dịch vụ lưu trữ ảnh đám mây Cloudinary. Màn hình nhân viên hiển thị lưới danh sách tài khoản kèm hồ sơ và vai trò, biểu mẫu thêm hoặc sửa ở bên cạnh, và khu vực gán quyền cho vai trò dưới dạng các hộp chọn theo từng quyền.')
    h2(doc, '2.12. Quy tắc kiểm tra dữ liệu nhập (Validation)')
    para(doc, 'Mọi dữ liệu người dùng nhập đều được kiểm tra hai lớp: lớp thứ nhất ở ViewModel để phản hồi ngay cho người dùng, lớp thứ hai ở Service và cơ sở dữ liệu (ràng buộc khóa, kiểu dữ liệu) để bảo đảm toàn vẹn dù dữ liệu đi qua đường nào. Bảng sau tổng hợp các quy tắc chính.')
    table(doc, 'Bảng 2.12.1. Quy tắc kiểm tra dữ liệu nhập',
          ['Trường dữ liệu', 'Quy tắc', 'Thông báo khi vi phạm'],
          [['Tên đăng nhập', 'Bắt buộc, không trùng với tài khoản khác', 'Tên đăng nhập đã tồn tại'],
           ['Mật khẩu', 'Bắt buộc khi tạo mới, đủ độ dài tối thiểu', 'Mật khẩu không hợp lệ'],
           ['Số bàn', 'Bắt buộc, không trùng', 'Số bàn đã tồn tại'],
           ['Tên loại bàn', 'Bắt buộc, không trùng', 'Tên loại bàn đã tồn tại'],
           ['Tên sản phẩm', 'Bắt buộc, không trùng', 'Tên sản phẩm đã tồn tại'],
           ['Giá bán, đơn giá giờ', 'Là số, lớn hơn 0', 'Giá phải lớn hơn 0'],
           ['Số lượng', 'Số nguyên dương', 'Số lượng không hợp lệ'],
           ['Khung giờ', 'Giờ bắt đầu và kết thúc hợp lệ', 'Khung giờ không hợp lệ'],
           ['Khoảng hiệu lực', 'Ngày kết thúc không trước ngày bắt đầu', 'Khoảng ngày không hợp lệ']],
          widths=[3.6, 5.8, 4.8], size=11.5)
    h2(doc, '2.13. Mô tả mục đích và quan hệ giữa các bảng dữ liệu')
    para(doc, 'Cơ sở dữ liệu gồm 17 bảng được nhóm theo năm nhóm chức năng. Mô tả dưới đây giúp người đọc nắm được vai trò của từng bảng trước khi xem chi tiết các trường ở mục 2.3.')
    table(doc, 'Bảng 2.13.1. Phân nhóm 17 bảng dữ liệu',
          ['Nhóm', 'Bảng', 'Mục đích'],
          [['Bảo mật và phân quyền', 'Roles, Users, Permissions, RolePermissions', 'Lưu vai trò, tài khoản, quyền và quan hệ nhiều-nhiều giữa vai trò với quyền'],
           ['Bàn chơi và giá', 'TableTypes, Tables, TablePricingRules, SessionPricingDetails', 'Lưu loại bàn, từng bàn, quy tắc giá và chi tiết giá đã áp dụng cho từng phiên'],
           ['Sản phẩm', 'ProductCategories, Products', 'Lưu danh mục và sản phẩm dịch vụ'],
           ['Con người', 'Customers, Employees', 'Lưu hồ sơ khách hàng và nhân viên'],
           ['Giao dịch', 'Sessions, SessionProducts, Invoices, InvoiceDetails', 'Lưu phiên chơi, sản phẩm gọi theo phiên, hóa đơn và chi tiết hóa đơn'],
           ['Giám sát', 'AuditLogs', 'Lưu nhật ký thao tác của người dùng']],
          widths=[3.4, 5.0, 5.8], size=11.5)
    para(doc, 'Các quan hệ quan trọng gồm: một vai trò có nhiều tài khoản; vai trò và quyền liên kết nhiều-nhiều thông qua bảng RolePermissions; một loại bàn có nhiều bàn và nhiều quy tắc giá; một bàn có nhiều phiên chơi theo thời gian nhưng tại một thời điểm chỉ có tối đa một phiên đang mở; một phiên có nhiều sản phẩm và nhiều bản ghi chi tiết giá; một hóa đơn có nhiều dòng chi tiết. Việc dùng khóa ngoại bảo đảm không tồn tại bản ghi mồ côi, ví dụ không thể có chi tiết hóa đơn mà không có hóa đơn.')
    h3(doc, '2.13.1. Chiến lược chỉ mục và tối ưu truy vấn')
    para(doc, 'Các cột khóa chính được đánh chỉ mục mặc định. Các cột thường xuyên dùng để tìm kiếm hoặc nối bảng như tên đăng nhập (duy nhất), số bàn (duy nhất), mã loại bàn và mã phiên được ưu tiên có chỉ mục để rút ngắn thời gian truy vấn. Do quy mô dữ liệu của một quán nhỏ nên chiến lược này là đủ; khi dữ liệu tăng lớn, có thể bổ sung chỉ mục phức hợp cho các truy vấn báo cáo theo thời gian.')


def c3(doc):
    h2(doc, '3.7. Quy trình phát triển, phân công và quản lý mã nguồn')
    h3(doc, '3.7.1. Quy trình thực hiện đề tài')
    para(doc, 'Nhóm thực hiện đề tài theo quy trình lặp, chia thành các giai đoạn: khảo sát và xác định yêu cầu; thiết kế kiến trúc và cơ sở dữ liệu; hiện thực từng phân hệ theo thứ tự ưu tiên (xác thực và phân quyền, quản lý bàn và giá, sản phẩm, nhân sự); kiểm thử; hoàn thiện báo cáo. Ở mỗi vòng lặp, hai thành viên họp ngắn để thống nhất công việc, trao đổi vướng mắc và xem xét mã của nhau.')
    table(doc, 'Bảng 3.7.1. Các giai đoạn thực hiện đề tài',
          ['Giai đoạn', 'Nội dung', 'Sản phẩm đầu ra'],
          [['1. Khảo sát', 'Tìm hiểu nghiệp vụ, xác định yêu cầu', 'Danh sách yêu cầu chức năng và phi chức năng'],
           ['2. Thiết kế', 'Kiến trúc MVVM, thiết kế CSDL 17 bảng, thiết kế giao diện', 'Sơ đồ ERD, tài liệu cơ sở dữ liệu, bản phác giao diện'],
           ['3. Khởi tạo hạ tầng', 'Tạo dịch vụ MySQL trên Aiven, chạy kịch bản tạo bảng', 'Cơ sở dữ liệu và dữ liệu mẫu'],
           ['4. Lập trình', 'Hiện thực các tầng Model, Service, ViewModel, View', 'Ứng dụng WPF chạy được'],
           ['5. Kiểm thử', 'Chạy ca kiểm thử, sửa lỗi', 'Bảng kết quả kiểm thử'],
           ['6. Báo cáo', 'Tổng hợp tài liệu, vẽ sơ đồ, viết báo cáo', 'Báo cáo bài tập lớn']],
          widths=[3.0, 6.2, 5.0], size=11.5)
    h3(doc, '3.7.2. Quản lý phiên bản bằng Git')
    para(doc, 'Mã nguồn được quản lý bằng Git. Tệp .gitignore loại trừ thư mục sinh ra khi biên dịch (bin, obj), thư mục cấu hình của Visual Studio và đặc biệt là tệp connections.config chứa thông tin kết nối thật. Nhờ vậy kho mã nguồn có thể chia sẻ an toàn giữa hai thành viên. Mỗi thành viên làm việc trên các thư mục khác nhau (một người phụ trách tầng nghiệp vụ bàn, giá và đơn hàng; người còn lại phụ trách xác thực, phân quyền, nhật ký) để hạn chế xung đột khi hợp nhất.')
    h2(doc, '3.8. Mô tả chi tiết cách hiện thực các chức năng chính')
    h3(doc, '3.8.1. Luồng xử lý đăng nhập')
    para(doc, 'Khi người dùng nhấn nút Đăng nhập, lệnh của LoginViewModel (kiểu AsyncRelayCommand) được kích hoạt. Lệnh này trước hết kiểm tra các trường nhập; nếu hợp lệ sẽ gọi AuthService.LoginAsync. Dịch vụ truy vấn bảng Users theo tên đăng nhập, bỏ qua các tài khoản không hoạt động, sau đó dùng thư viện BCrypt để so khớp mật khẩu nhập với giá trị băm lưu trong cơ sở dữ liệu. Khi khớp, dịch vụ lấy danh sách quyền theo vai trò và gọi UserSession.SetSession để lưu thông tin phiên. Cuối cùng, một bản ghi nhật ký được ghi và cửa sổ chính được mở. Mọi bước truy vấn đều bất đồng bộ nên cửa sổ đăng nhập vẫn phản hồi trong lúc chờ.')
    h3(doc, '3.8.2. Luồng xử lý phân quyền')
    para(doc, 'Sau khi đăng nhập, MainViewModel đọc danh sách quyền từ UserSession để quyết định mục điều hướng nào được hiển thị. Đây là lớp bảo vệ ở giao diện, giúp người dùng chỉ thấy những gì họ được phép dùng. Tuy nhiên, vì việc ẩn nút không đủ để bảo đảm an toàn, các phương thức nghiệp vụ nhạy cảm còn gọi UserSession.DemandPermission ở đầu phương thức; nếu thiếu quyền thì ngoại lệ được ném ra và thao tác bị chặn. Cách làm hai lớp này bảo đảm ngay cả khi giao diện có lỗi hoặc bị gọi theo đường khác, quyền hạn vẫn được tôn trọng.')
    h3(doc, '3.8.3. Luồng xử lý hiển thị thời gian thực của bàn')
    para(doc, 'TableManagementViewModel nạp danh sách bàn từ TableService, trong đó các bàn đang chơi mang theo thời điểm bắt đầu của phiên. Một bộ định thời gian trên luồng giao diện được thiết lập để định kỳ gọi UpdateElapsed của từng bàn đang chơi. Phương thức này tính khoảng chênh lệch giữa thời điểm hiện tại và thời điểm bắt đầu rồi phát sự kiện thay đổi thuộc tính, nhờ liên kết dữ liệu mà bộ đếm trên thẻ bàn tự cập nhật mà không cần can thiệp thủ công vào giao diện. Cách làm này nhẹ vì không phải truy vấn cơ sở dữ liệu mỗi giây.')
    h3(doc, '3.8.4. Luồng xử lý quản lý quy tắc giá')
    para(doc, 'PricingRuleService cung cấp các thao tác thêm, sửa, ngừng áp dụng quy tắc giá và phương thức GetEffectiveHourlyRateAsync để xác định đơn giá hiệu lực tại một thời điểm. Phương thức này duyệt các quy tắc đang hoạt động của loại bàn, lọc theo ngày hiệu lực, loại ngày và khung giờ, rồi chọn quy tắc có độ ưu tiên cao nhất. Việc đặt logic chọn giá ở tầng dịch vụ thay vì rải trong giao diện giúp đảm bảo mọi nơi cần giá đều dùng chung một quy tắc, tránh hiển thị mỗi nơi một giá.')
    h3(doc, '3.8.5. Luồng xử lý quản lý sản phẩm và hình ảnh')
    para(doc, 'ProductService quản lý danh mục và sản phẩm, kiểm tra trùng tên trước khi lưu. Hình ảnh sản phẩm được tải lên dịch vụ Cloudinary thông qua CloudinaryService; cơ sở dữ liệu chỉ lưu đường dẫn ảnh, giúp giảm dung lượng lưu trữ và tăng tốc truy vấn. Nếu chưa cấu hình tài khoản Cloudinary, ứng dụng vẫn hoạt động bình thường mà không có chức năng tải ảnh.')
    h3(doc, '3.8.6. Luồng xử lý quản lý nhân viên')
    para(doc, 'UserService xử lý thêm, sửa, khóa tài khoản kèm hồ sơ nhân viên. Khi tạo hoặc đặt lại mật khẩu, mật khẩu được băm bằng BCrypt trước khi ghi xuống cơ sở dữ liệu. Việc tạo tài khoản và hồ sơ nhân viên thực hiện trong cùng một giao dịch để tránh trường hợp có tài khoản mà thiếu hồ sơ. Mỗi thao tác đều được ghi vào nhật ký kiểm toán thông qua AuditLogService.')
    h2(doc, '3.9. Hướng dẫn sử dụng phần mềm')
    para(doc, 'Phần này mô tả cách sử dụng hệ thống theo các tình huống thường gặp trong một ca trực, giúp nhân viên mới nhanh chóng làm quen.')
    h3(doc, '3.9.1. Bắt đầu ca làm việc')
    bullets(doc, [
        'Mở ứng dụng, nhập tên đăng nhập và mật khẩu được cấp, nhấn Đăng nhập.',
        'Có thể tick Ghi nhớ để lần sau không phải gõ lại tên đăng nhập.',
        'Quan sát sơ đồ bàn: bàn trống, bàn đang chơi, bàn đặt trước và bàn bảo trì được phân biệt bằng màu sắc.'])
    h3(doc, '3.9.2. Phục vụ một lượt khách')
    bullets(doc, [
        'Khi khách đến, chọn bàn trống phù hợp rồi nhấn Mở bàn; bộ đếm thời gian bắt đầu chạy.',
        'Khi khách gọi đồ, chọn bàn, mở danh sách sản phẩm, chọn món và số lượng.',
        'Nếu khách muốn đổi bàn, dùng chức năng Chuyển bàn và chọn bàn đích đang trống.',
        'Khi khách thanh toán, chọn bàn và nhấn Thanh toán; kiểm tra tiền giờ, tiền dịch vụ và tổng tiền trước khi xác nhận.',
        'Sau khi xác nhận, bàn trở về trạng thái Trống và sẵn sàng đón khách mới.'])
    h3(doc, '3.9.3. Các thao tác dành cho quản trị viên')
    bullets(doc, [
        'Thêm hoặc sửa bàn và loại bàn ở màn hình quản lý bàn.',
        'Thiết lập giá nền, giá theo khung giờ và khuyến mại ở phần quy tắc giá.',
        'Thêm danh mục và sản phẩm, tải ảnh sản phẩm ở màn hình sản phẩm.',
        'Thêm nhân viên, khóa tài khoản, đặt lại mật khẩu và điều chỉnh quyền ở màn hình nhân viên.'])
    h3(doc, '3.9.4. Xử lý sự cố thường gặp')
    table(doc, 'Bảng 3.9.1. Sự cố thường gặp và cách xử lý',
          ['Sự cố', 'Nguyên nhân có thể', 'Cách xử lý'],
          [['Không đăng nhập được, báo lỗi kết nối', 'Mất Internet hoặc sai cấu hình chuỗi kết nối', 'Kiểm tra mạng; kiểm tra connections.config; thử lại'],
           ['Đăng nhập báo sai mật khẩu', 'Nhập sai hoặc tài khoản bị khóa', 'Nhập lại; liên hệ quản trị viên để kiểm tra tài khoản'],
           ['Không thấy một số chức năng', 'Tài khoản không có quyền tương ứng', 'Liên hệ quản trị viên để gán quyền'],
           ['Ký tự tiếng Việt hiển thị sai', 'Sai mã hóa ký tự', 'Bảo đảm chuỗi kết nối có CharSet=utf8mb4'],
           ['Thao tác lưu bị lỗi trùng dữ liệu', 'Trùng số bàn, tên sản phẩm hoặc tên đăng nhập', 'Đổi sang giá trị khác']],
          widths=[4.2, 4.8, 5.2], size=11.5)
    h2(doc, '3.10. Đánh giá hệ thống theo các tiêu chí chất lượng phần mềm')
    para(doc, 'Hệ thống được đánh giá dựa trên các đặc tính chất lượng của tiêu chuẩn ISO/IEC 25010. Đánh giá mang tính định tính, dựa trên kết quả kiểm thử và quá trình sử dụng thử của nhóm.')
    table(doc, 'Bảng 3.10.1. Đánh giá theo đặc tính chất lượng ISO/IEC 25010',
          ['Đặc tính', 'Nhận xét', 'Mức đánh giá'],
          [['Phù hợp chức năng', 'Đáp ứng các chức năng cốt lõi của bài toán quản lý bàn, giá, dịch vụ, nhân sự', 'Tốt'],
           ['Hiệu năng', 'Phản hồi nhanh khi mạng ổn định; truy vấn bất đồng bộ giúp giao diện mượt', 'Khá'],
           ['Tương thích', 'Chạy trên Windows 10 và 11; kết nối MySQL chuẩn', 'Khá'],
           ['Khả năng sử dụng', 'Giao diện trực quan, tiếng Việt, ít thao tác', 'Tốt'],
           ['Độ tin cậy', 'Dùng giao dịch và khóa ngoại; phụ thuộc vào kết nối Internet', 'Khá'],
           ['Bảo mật', 'Băm BCrypt, SSL, phân quyền hai lớp, nhật ký kiểm toán', 'Tốt'],
           ['Khả năng bảo trì', 'Kiến trúc MVVM, tách tầng rõ ràng, dễ mở rộng', 'Tốt'],
           ['Khả năng chuyển đổi', 'Cần cài .NET Framework; chưa có bản đóng gói tự động', 'Trung bình']],
          widths=[3.8, 7.6, 2.8], size=11.5)
    h2(doc, '3.11. Phân tích điểm mạnh, điểm yếu, cơ hội và thách thức (SWOT)')
    table(doc, 'Bảng 3.11.1. Phân tích SWOT của hệ thống',
          ['Yếu tố', 'Nội dung'],
          [['Điểm mạnh', 'Đúng nghiệp vụ quán Billiard; kiến trúc rõ ràng; dữ liệu tập trung trên đám mây; bảo mật tốt; chi phí thấp'],
           ['Điểm yếu', 'Phụ thuộc Internet; chưa có báo cáo thống kê nâng cao; chưa tích hợp thiết bị phần cứng'],
           ['Cơ hội', 'Nhu cầu số hóa quán nhỏ tăng; có thể mở rộng thành nền tảng nhiều chi nhánh; có thể thêm ứng dụng di động'],
           ['Thách thức', 'Cạnh tranh từ các phần mềm có sẵn; yêu cầu độ ổn định cao vì liên quan trực tiếp đến tiền; người dùng cần được đào tạo']],
          widths=[3.0, 11.2], size=11.5)
