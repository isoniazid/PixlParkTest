/* eslint-disable no-unused-vars */
import { useState } from 'react';
import RequestSender from './RequestSender';
import './styles/common.css';
import PrivatePage from './components/PrivatePage';



function App() {
  const [email, setEmail] = useState('');
  const [secretCode, setSecretCode] = useState('');
  const [showSecretCode, setShowSecretCode] = useState(false);
  const [result, setResult] = useState('');
  const [showForm, setShowForm] = useState(true);
  let accessToken = null;

  //Обработка результата запроса
  const handleResponseResult = async (status, data) => {

    if (status === 200) {
      //Отправлен запрос на получение кода
      if (!showSecretCode) {
        setResult(`На вашу почту ${email} был выслан код`);
        setShowSecretCode(true);
        return;
      }

      //Отправлен валидный код
      if (accessToken === null) {
        accessToken = JSON.parse(data);
        let requestResult = await RequestSender.GetPrivateAsync(accessToken);
        handleResponseResult(requestResult.status, requestResult.data);
        return;
      }

      //Отправлен валидный токен 
      setResult(<>Получен защищенный контент:<br /><span>{JSON.parse(data)}</span></>);
      setShowForm(false);
      return;

    }

    let parsedJson = JSON.parse(data);

    //Ошибки валидации
    if (status === 422) {
      setResult(<ul>Ошибка:
        {parsedJson.map(item => (
          <li key={item.key}>{item.value}</li>
        ))}
      </ul>);
      return;
    }

    //Не авторизован
    if (status === 401) {
      setResult("Токен не валиден");
    }

    //прочие ошибки
    setResult(`Ошибка: ${parsedJson.detail}`);
  }


  //Обработчик нажатия кнопки
  const handleSendClick = async (event) => {

    event.preventDefault();

    let requestResult;

    //Отправка запроса на токена
    if (showSecretCode) {
      requestResult = await RequestSender.RegisterAsync(email, secretCode)
      handleResponseResult(requestResult.status, requestResult.data);
      return;
    }

    //отправка запроса на получение кода
    requestResult = await RequestSender.ApplyEmailAsync(email);

    handleResponseResult(requestResult.status, requestResult.data);

  };



  return (!showForm ? <PrivatePage text={result} /> :
    <main>
      <div className='login-form'>
        <h1>Вход</h1>
        <h2>{result}</h2>
        <form>
          <input
            type="email"
            placeholder="Введи email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={showSecretCode}
          />
          {showSecretCode && (
            <input
              type="text"
              placeholder="Введи код"
              value={secretCode}
              onChange={(e) => setSecretCode(e.target.value)}
            />
          )}
          <button id="send" onClick={handleSendClick}>
            Отправить
          </button>
        </form>
      </div></main>
  );
}

export default App;