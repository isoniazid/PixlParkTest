class RequestSender {

    //Запрос на отправку кода
    static async ApplyEmailAsync(email) {
        const url = `http://localhost:5000/api/v1/apply?email=${email}`;
        const requestOptions = { method: 'POST' };


        const response = await fetch(url, requestOptions);
        const data = await response.text();

        return { status: response.status, data: data};
    }

    //Запрос на получение токена
    static async RegisterAsync(email, secretCode) {
        const url = `http://localhost:5000/api/v1/register?email=${email}&code=${secretCode}`
        const requestOptions = { method: 'POST' };

        const response = await fetch(url, requestOptions);
        const data = await response.text();

        return { status: response.status, data: data};
    }

    //Запрос к защищенному содержимому
    static async GetPrivateAsync(token) {
        const url = 'http://localhost:5000/api/v1/dummy';
        const requestOptions = {
            method: 'Get',
            headers: {
                'Authorization': `Bearer ${token}`
            }};

        const response = await fetch(url, requestOptions);
        const data = await response.text();

        return { status: response.status, data: data};
    }
}

export default RequestSender;