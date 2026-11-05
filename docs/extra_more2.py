# -*- coding: utf-8 -*-
"""Bổ sung văn bản đợt 2 cho chương 1, 2, 3."""
from extra_content import para, h2, h3, bullets, table


def c1(doc):
    h2(doc, '1.11. Mô tả chi tiết các quy trình nghiệp vụ cần số hóa')
    h3(doc, '1.11.1. Quy trình mở bàn và theo dõi phiên chơi')
    para(doc, 'Quy trình bắt đầu khi khách chọn bàn. Thu ngân kiểm tra bàn đó đang ở trạng thái trống, sau đó khởi tạo phiên chơi, ghi nhận thời điểm bắt đầu và chuyển trạng thái bàn sang đang chơi. Trong suốt phiên, mọi sự kiện phát sinh gồm gọi thêm dịch vụ, chuyển bàn hoặc tạm dừng đều được gắn vào cùng một phiên để đảm bảo tính liên tục của dữ liệu. Khi hệ thống theo dõi giờ một cách tự động, nhân viên không còn phải nhớ giờ khách vào, đồng thời chủ quán có căn cứ rõ ràng nếu khách thắc mắc về thời gian.')
    h3(doc, '1.11.2. Quy trình gọi và phục vụ dịch vụ ăn uống')
    para(doc, 'Khi khách gọi món, nhân viên phục vụ báo cho thu ngân hoặc tự thao tác thêm sản phẩm vào phiên chơi của bàn tương ứng. Hệ thống ghi nhận số lượng và đơn giá tại thời điểm gọi. Việc lưu đơn giá theo thời điểm là yêu cầu quan trọng: nếu chủ quán điều chỉnh giá bán sau đó, các phiên đang diễn ra và các hóa đơn đã lập không bị thay đổi, tránh sai lệch số liệu và tranh chấp với khách.')
    h3(doc, '1.11.3. Quy trình thanh toán')
    para(doc, 'Khi khách yêu cầu thanh toán, hệ thống xác định thời điểm kết thúc, tính tiền giờ theo quy tắc giá hiệu lực, cộng tiền dịch vụ và hiển thị tổng số tiền kèm chi tiết từng khoản để thu ngân đọc cho khách. Sau khi thu tiền, thu ngân xác nhận để hệ thống lưu hóa đơn, kết thúc phiên và giải phóng bàn. Toàn bộ các thao tác ghi dữ liệu được thực hiện trong một giao dịch để không xảy ra tình trạng tiền đã thu mà bàn vẫn bị treo hoặc ngược lại.')
    h3(doc, '1.11.4. Quy trình quản trị và giám sát')
    para(doc, 'Chủ quán định kỳ thực hiện các công việc quản trị gồm điều chỉnh bảng giá theo mùa vụ, cập nhật thực đơn, thêm hoặc khóa tài khoản nhân viên và xem lại nhật ký thao tác khi cần đối chiếu. Hệ thống cung cấp các màn hình riêng cho từng việc, đồng thời giới hạn các chức năng này chỉ dành cho người có quyền, nhằm bảo vệ cấu hình quan trọng của quán khỏi những thay đổi ngoài ý muốn.')
    h2(doc, '1.12. Mô hình dữ liệu khái niệm')
    para(doc, 'Từ khảo sát nghiệp vụ, nhóm xác định các thực thể chính của bài toán cùng mối quan hệ giữa chúng. Mô hình khái niệm này là bước trung gian giữa yêu cầu và thiết kế cơ sở dữ liệu vật lý ở chương 2.')
    table(doc, 'Bảng 1.12.1. Các thực thể và mối quan hệ khái niệm',
          ['Thực thể', 'Ý nghĩa', 'Quan hệ chính'],
          [['Người dùng', 'Tài khoản đăng nhập hệ thống', 'Thuộc một vai trò; gắn với một hồ sơ nhân viên'],
           ['Vai trò', 'Nhóm quyền như Admin, Thu ngân', 'Có nhiều quyền; có nhiều người dùng'],
           ['Quyền', 'Một khả năng cụ thể như quản lý bàn, quản lý giá', 'Thuộc nhiều vai trò'],
           ['Loại bàn', 'Pool, Carom, Snooker...', 'Có nhiều bàn và nhiều quy tắc giá'],
           ['Bàn', 'Một bàn vật lý trong quán', 'Thuộc một loại; có nhiều phiên chơi'],
           ['Quy tắc giá', 'Giá nền hoặc khuyến mại theo khung giờ', 'Thuộc một loại bàn'],
           ['Phiên chơi', 'Một lượt sử dụng bàn của khách', 'Thuộc một bàn; có nhiều sản phẩm; sinh ra hóa đơn'],
           ['Sản phẩm', 'Món dịch vụ bán kèm', 'Thuộc một danh mục; xuất hiện trong nhiều phiên'],
           ['Hóa đơn', 'Chứng từ thanh toán', 'Có nhiều dòng chi tiết; có thể gắn khách hàng'],
           ['Khách hàng', 'Người chơi được ghi nhận', 'Có nhiều hóa đơn'],
           ['Nhật ký', 'Lịch sử thao tác', 'Gắn với người dùng thực hiện']],
          widths=[3.0, 5.2, 6.0], size=11.5)
    para(doc, 'Mô hình trên thể hiện rõ hai trục dữ liệu của hệ thống. Trục thứ nhất là trục cấu hình, gồm vai trò, quyền, loại bàn, bàn, quy tắc giá, danh mục và sản phẩm, thay đổi tương đối ít và do quản trị viên quản lý. Trục thứ hai là trục giao dịch, gồm phiên chơi, sản phẩm theo phiên, hóa đơn và nhật ký, phát sinh liên tục theo hoạt động hằng ngày và do thu ngân tạo ra. Việc tách hai trục giúp thiết kế quyền hạn và chiến lược lưu trữ phù hợp cho từng loại dữ liệu.')


def c2(doc):
    h2(doc, '2.14. Cơ sở lý thuyết và công nghệ sử dụng')
    h3(doc, '2.14.1. Ngôn ngữ C# và nền tảng .NET Framework')
    para(doc, 'C# là ngôn ngữ lập trình hướng đối tượng, kiểu tĩnh mạnh do Microsoft phát triển, hỗ trợ nhiều tính năng hiện đại như thuộc tính, sự kiện, delegate, LINQ và mô hình lập trình bất đồng bộ async/await. .NET Framework là nền tảng chạy ứng dụng trên Windows, gồm môi trường thực thi CLR chịu trách nhiệm quản lý bộ nhớ và biên dịch JIT, cùng thư viện lớp cơ sở (BCL) phong phú. Đề tài sử dụng .NET Framework 4.7.2 vì tương thích rộng với các máy Windows đang dùng trong thực tế và đầy đủ thư viện cho ứng dụng desktop.')
    h3(doc, '2.14.2. Windows Presentation Foundation (WPF) và XAML')
    para(doc, 'WPF là nền tảng xây dựng giao diện desktop dựa trên đồ họa vector, độc lập độ phân giải, cho phép giao diện co giãn tốt trên nhiều kích thước màn hình. Giao diện WPF được mô tả bằng XAML, một ngôn ngữ đánh dấu dạng XML, tách phần trình bày khỏi mã xử lý. WPF cung cấp hệ thống liên kết dữ liệu (data binding), kiểu dáng (style), mẫu (template) và lệnh (command) rất phù hợp để áp dụng mô hình MVVM.')
    h3(doc, '2.14.3. Mô hình kiến trúc MVVM')
    para(doc, 'MVVM chia ứng dụng thành ba thành phần. Model biểu diễn dữ liệu và quy tắc nghiệp vụ. View là giao diện XAML. ViewModel đóng vai trò cầu nối: cung cấp dữ liệu và lệnh để View liên kết, đồng thời không biết gì về chi tiết cài đặt giao diện. Lợi ích chính của MVVM là khả năng kiểm thử ViewModel độc lập với giao diện, khả năng thay đổi giao diện mà không ảnh hưởng logic và việc phân công công việc rõ ràng giữa người thiết kế giao diện và người lập trình nghiệp vụ.')
    h3(doc, '2.14.4. Liên kết dữ liệu và cơ chế thông báo thay đổi')
    para(doc, 'Cơ chế liên kết dữ liệu của WPF dựa trên giao diện INotifyPropertyChanged: khi một thuộc tính của đối tượng thay đổi, đối tượng phát sự kiện PropertyChanged và các thành phần giao diện đang liên kết với thuộc tính đó tự cập nhật. Lớp ObservableObject của đề tài đóng gói cơ chế này để các Model và ViewModel dùng lại. Với danh sách, kiểu ObservableCollection thông báo khi phần tử được thêm hoặc xóa, giúp lưới và danh sách trên giao diện luôn đồng bộ với dữ liệu.')
    h3(doc, '2.14.5. Mẫu Command và lập trình bất đồng bộ')
    para(doc, 'Thay vì viết xử lý sự kiện nhấn nút trong mã phía sau giao diện, MVVM dùng mẫu Command thông qua giao diện ICommand. Mỗi lệnh gồm hành động thực thi và điều kiện cho phép thực thi; khi điều kiện sai, nút tự bị vô hiệu hóa. Với các lệnh làm việc với cơ sở dữ liệu, đề tài dùng AsyncRelayCommand dựa trên Task và async/await để không chặn luồng giao diện, đồng thời ngăn người dùng nhấn lặp khi lệnh còn đang chạy.')
    h3(doc, '2.14.6. Hệ quản trị cơ sở dữ liệu MySQL')
    para(doc, 'MySQL là hệ quản trị cơ sở dữ liệu quan hệ mã nguồn mở phổ biến, hỗ trợ giao dịch ACID thông qua engine InnoDB, khóa ngoại, chỉ mục và tập ký tự utf8mb4 đầy đủ cho tiếng Việt. Phiên bản 8.0 bổ sung nhiều cải tiến về hiệu năng, bảo mật và hỗ trợ biểu thức bảng chung (CTE). Đây là lựa chọn phù hợp cho các ứng dụng quản lý vừa và nhỏ nhờ cân bằng giữa hiệu năng, độ tin cậy và chi phí.')
    h3(doc, '2.14.7. Dịch vụ cơ sở dữ liệu đám mây Aiven')
    para(doc, 'Aiven là nền tảng cung cấp các dịch vụ dữ liệu được quản lý hoàn toàn trên đám mây. Với MySQL của Aiven, nhà cung cấp chịu trách nhiệm cài đặt, vá lỗi, sao lưu định kỳ, giám sát và mã hóa kênh truyền, người dùng chỉ cần kết nối bằng thông tin được cấp. Nhờ đó nhóm không phải tự vận hành máy chủ, đồng thời nhiều máy trạm trong quán có thể cùng truy cập một nguồn dữ liệu thống nhất. Đổi lại, hệ thống phụ thuộc vào kết nối Internet và cần cấu hình SSL khi kết nối.')
    h3(doc, '2.14.8. Thư viện MySqlConnector và ADO.NET')
    para(doc, 'ADO.NET là tập các lớp chuẩn của .NET để làm việc với cơ sở dữ liệu quan hệ, gồm kết nối, lệnh, trình đọc dữ liệu và giao dịch. MySqlConnector là thư viện triển khai ADO.NET cho MySQL với hỗ trợ bất đồng bộ thực sự, hiệu năng cao và hỗ trợ đầy đủ các tùy chọn SSL. Việc dùng câu lệnh có tham số (parameterized query) trong mọi truy vấn giúp ngăn chặn tấn công chèn mã SQL (SQL Injection).')
    h3(doc, '2.14.9. Thuật toán băm mật khẩu BCrypt')
    para(doc, 'BCrypt là hàm băm mật khẩu có chủ đích chậm, có tham số chi phí (work factor) điều chỉnh được và tự sinh một chuỗi muối (salt) ngẫu nhiên cho mỗi mật khẩu. Hai đặc tính này làm cho các cuộc tấn công dò mật khẩu bằng bảng băm dựng sẵn hoặc vét cạn trở nên rất tốn kém. Kết quả băm đã chứa sẵn muối và tham số chi phí nên việc xác thực chỉ cần so khớp mật khẩu nhập với chuỗi băm đã lưu.')
    h3(doc, '2.14.10. Giao thức SSL/TLS')
    para(doc, 'SSL/TLS là giao thức mã hóa đường truyền giữa máy khách và máy chủ, bảo đảm tính bí mật, toàn vẹn và xác thực của dữ liệu trao đổi. Khi chuỗi kết nối đặt SslMode=Required, thư viện từ chối kết nối nếu máy chủ không hỗ trợ mã hóa, nhờ đó thông tin đăng nhập và dữ liệu nghiệp vụ không bị nghe lén khi đi qua mạng Internet công cộng.')
    h3(doc, '2.14.11. Thư viện giao diện Material Design và dịch vụ Cloudinary')
    para(doc, 'Material Design in XAML Toolkit cung cấp bộ điều khiển và chủ đề giao diện theo ngôn ngữ thiết kế Material, giúp ứng dụng có diện mạo hiện đại, nhất quán mà không phải tự vẽ từng thành phần. Cloudinary là dịch vụ lưu trữ và phân phối hình ảnh trên đám mây; ứng dụng tải ảnh sản phẩm lên Cloudinary và chỉ lưu đường dẫn trong cơ sở dữ liệu, nhờ vậy giảm tải cho cơ sở dữ liệu quan hệ và tăng tốc hiển thị ảnh.')
    h2(doc, '2.15. Mô tả luồng tương tác giữa các thành phần')
    h3(doc, '2.15.1. Luồng đăng nhập')
    para(doc, 'Người dùng nhập thông tin trên LoginWindow. Thông tin được liên kết hai chiều với LoginViewModel. Khi nhấn nút, lệnh đăng nhập của ViewModel gọi AuthService. AuthService mở kết nối thông qua DatabaseHelper, truy vấn tài khoản, so khớp mật khẩu bằng BCrypt và trả về đối tượng người dùng. ViewModel lưu phiên vào UserSession, sau đó yêu cầu mở MainWindow. Ở mọi bước lỗi, ViewModel nhận ngoại lệ hoặc giá trị rỗng và hiển thị thông báo phù hợp trên giao diện.')
    h3(doc, '2.15.2. Luồng mở bàn')
    para(doc, 'Thu ngân chọn bàn trên TableManagementView. Lệnh mở bàn của TableManagementViewModel kiểm tra quyền qua UserSession, gọi TableService để tạo phiên chơi và cập nhật trạng thái bàn trong một giao dịch. Khi dịch vụ trả về thành công, ViewModel cập nhật đối tượng bàn tương ứng; nhờ liên kết dữ liệu, thẻ bàn tự đổi màu và bộ đếm thời gian bắt đầu chạy. Cuối cùng, AuditLogService ghi lại thao tác.')
    h3(doc, '2.15.3. Luồng thanh toán')
    para(doc, 'Khi thu ngân chọn thanh toán, ViewModel gọi PricingRuleService để xác định đơn giá giờ theo từng khung thời gian của phiên, ProductService để lấy các sản phẩm đã gọi, rồi tổng hợp thành tiền giờ, tiền dịch vụ và tổng thanh toán. Sau khi thu ngân xác nhận, dịch vụ ghi hóa đơn và chi tiết hóa đơn, đóng phiên và trả bàn về trạng thái trống trong cùng một giao dịch. Nếu bất kỳ bước nào thất bại, giao dịch được hoàn tác và giao diện nhận thông báo lỗi.')
    h2(doc, '2.16. Thiết kế bảo mật bổ sung')
    h3(doc, '2.16.1. Phòng chống tấn công chèn mã SQL')
    para(doc, 'Mọi câu lệnh truy vấn đều sử dụng tham số thay vì ghép chuỗi trực tiếp từ dữ liệu nhập. Nhờ đó, dù người dùng nhập các ký tự đặc biệt, nội dung vẫn được xem là dữ liệu chứ không phải một phần của câu lệnh, loại bỏ nguy cơ SQL Injection.')
    h3(doc, '2.16.2. Nguyên tắc đặc quyền tối thiểu')
    para(doc, 'Mỗi vai trò chỉ được cấp những quyền cần thiết cho công việc. Thu ngân không có quyền thay đổi bảng giá hay quản lý nhân sự; chỉ quản trị viên mới có đầy đủ các quyền. Nguyên tắc này làm giảm thiệt hại nếu một tài khoản bị lộ hoặc bị lạm dụng.')
    h3(doc, '2.16.3. Nhật ký kiểm toán và khả năng truy vết')
    para(doc, 'Các thao tác quan trọng như đăng nhập, đăng xuất, thêm, sửa, xóa dữ liệu cấu hình đều được ghi vào bảng AuditLogs cùng người thực hiện, loại và mã đối tượng tác động và mô tả. Khi phát sinh sự cố hoặc nghi ngờ gian lận, chủ quán có thể tra cứu để xác định ai đã làm gì và vào lúc nào.')
    h3(doc, '2.16.4. Quản lý bí mật cấu hình')
    para(doc, 'Thông tin kết nối cơ sở dữ liệu và khóa của dịch vụ ngoài được tách khỏi mã nguồn, không đưa lên kho Git. Khi triển khai, người quản trị hệ thống tự cung cấp các giá trị này trên từng máy. Đây là thực hành bảo mật cơ bản nhưng quan trọng để tránh lộ thông tin nhạy cảm.')


def c3(doc):
    h2(doc, '3.12. Phương pháp và kết quả kiểm thử phi chức năng')
    h3(doc, '3.12.1. Kiểm thử hiệu năng')
    para(doc, 'Kiểm thử hiệu năng tập trung vào thời gian phản hồi của các thao tác thường dùng khi kết nối tới cơ sở dữ liệu Aiven qua Internet: đăng nhập, nạp danh sách bàn, thêm sản phẩm vào phiên, thanh toán. Nhóm đo thời gian từ lúc người dùng thao tác đến khi giao diện cập nhật, lặp lại nhiều lần ở các thời điểm khác nhau và lấy giá trị trung bình. Kết quả cho thấy các thao tác đọc dữ liệu hoàn tất rất nhanh, các thao tác ghi dùng giao dịch chậm hơn đôi chút do cần nhiều lượt trao đổi với máy chủ nhưng vẫn ở mức chấp nhận được cho nghiệp vụ quán.')
    h3(doc, '3.12.2. Kiểm thử độ bền kết nối')
    para(doc, 'Nhóm thử các tình huống bất lợi về mạng: ngắt Internet khi đang đăng nhập, ngắt khi đang lưu dữ liệu, mạng chậm. Hệ thống không bị treo hoặc thoát đột ngột; thay vào đó hiển thị thông báo lỗi, và thao tác ghi dở dang được hoàn tác nhờ giao dịch nên dữ liệu không bị sai lệch. Sau khi mạng phục hồi, người dùng thao tác lại bình thường mà không cần khởi động lại ứng dụng.')
    h3(doc, '3.12.3. Kiểm thử bảo mật')
    table(doc, 'Bảng 3.12.1. Các hạng mục kiểm thử bảo mật',
          ['Hạng mục', 'Cách thực hiện', 'Kết quả'],
          [['Lưu trữ mật khẩu', 'Kiểm tra cột mật khẩu trong CSDL', 'Chỉ chứa chuỗi băm BCrypt, không có mật khẩu rõ'],
           ['Chèn mã SQL', 'Nhập chuỗi đặc biệt vào ô đăng nhập và tìm kiếm', 'Được xử lý như dữ liệu thường, không ảnh hưởng truy vấn'],
           ['Vượt quyền', 'Dùng tài khoản thu ngân thử thao tác quản trị', 'Bị chặn và thông báo thiếu quyền'],
           ['Lộ cấu hình', 'Rà soát kho Git', 'Không chứa tệp kết nối thật'],
           ['Mã hóa kênh truyền', 'Kết nối với SslMode=Required', 'Kết nối mã hóa thành công'],
           ['Tài khoản bị khóa', 'Đăng nhập bằng tài khoản đã khóa', 'Bị từ chối']],
          widths=[3.4, 5.6, 5.2], size=11.5)
    h2(doc, '3.13. Đối chiếu kết quả với yêu cầu đặt ra')
    table(doc, 'Bảng 3.13.1. Đối chiếu kết quả thực hiện với yêu cầu ban đầu',
          ['Yêu cầu', 'Kết quả', 'Mức hoàn thành'],
          [['Đăng nhập và xác thực an toàn', 'Hoàn thành với BCrypt và khóa tài khoản', 'Hoàn thành'],
           ['Phân quyền theo vai trò', 'Hoàn thành với 8 quyền và hai lớp kiểm tra', 'Hoàn thành'],
           ['Quản lý bàn và loại bàn', 'Hoàn thành', 'Hoàn thành'],
           ['Quản lý quy tắc giá', 'Hoàn thành giá nền, khung giờ, loại ngày, khuyến mại', 'Hoàn thành'],
           ['Quản lý sản phẩm và hình ảnh', 'Hoàn thành, hình ảnh qua Cloudinary', 'Hoàn thành'],
           ['Quản lý nhân viên', 'Hoàn thành', 'Hoàn thành'],
           ['Nhật ký kiểm toán', 'Hoàn thành ghi nhật ký thao tác', 'Hoàn thành'],
           ['Báo cáo thống kê doanh thu', 'Cơ sở dữ liệu đã sẵn sàng, giao diện báo cáo chưa hoàn thiện', 'Một phần'],
           ['Hệ thống khách hàng thân thiết', 'Có cấu trúc dữ liệu, chưa có giao diện đầy đủ', 'Một phần']],
          widths=[4.8, 6.4, 3.0], size=11.5)
    h2(doc, '3.14. Hướng phát triển chi tiết')
    h3(doc, '3.14.1. Hoàn thiện nghiệp vụ')
    bullets(doc, [
        'Xây dựng phân hệ báo cáo thống kê doanh thu theo ngày, tuần, tháng, theo loại bàn và theo nhân viên, kèm biểu đồ trực quan.',
        'Hoàn thiện quản lý ca làm việc: mở ca, chốt ca, đối soát tiền mặt và chuyển khoản.',
        'Xây dựng chức năng đặt bàn trước và nhắc lịch.',
        'Hoàn thiện chương trình khách hàng thân thiết: tích điểm, hạng thành viên, ưu đãi tự động.',
        'Hỗ trợ in hóa đơn nhiệt và tạo mã QR chuyển khoản.'])
    h3(doc, '3.14.2. Nâng cấp kỹ thuật')
    bullets(doc, [
        'Nâng cấp lên .NET hiện đại (.NET 8) để tận dụng hiệu năng và thời gian hỗ trợ dài hạn.',
        'Bổ sung bộ kiểm thử đơn vị tự động cho tầng Service và ViewModel.',
        'Thiết lập tích hợp liên tục (CI) để tự động biên dịch và kiểm thử khi có thay đổi.',
        'Bổ sung cơ chế lưu đệm và chế độ ngoại tuyến tạm thời để chịu được mất mạng ngắn.',
        'Xây dựng ứng dụng di động hoặc web cho chủ quán xem báo cáo từ xa.',
        'Đóng gói bộ cài tự động để triển khai nhanh trên máy mới.'])
    h2(doc, '3.15. Tổng kết chương')
    para(doc, 'Chương 3 đã trình bày giải pháp công nghệ, cách hiện thực các thành phần chính, hướng dẫn cài đặt và sử dụng, cũng như kết quả kiểm thử chức năng, hiệu năng và bảo mật. Kết quả cho thấy hệ thống đáp ứng phần lớn mục tiêu đã đặt ra: vận hành ổn định trên cơ sở dữ liệu đám mây, bảo mật theo thực hành tốt và có kiến trúc đủ rõ ràng để tiếp tục mở rộng. Những hạng mục chưa hoàn thiện được xác định cụ thể và đưa vào hướng phát triển ở phần Kết luận.')
