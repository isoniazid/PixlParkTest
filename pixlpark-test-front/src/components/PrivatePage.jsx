import PropTypes from 'prop-types'

function PrivatePage(props) {

    return (
        <div className="private-page"><h1>{props.text}</h1></div>
    );
}

PrivatePage.propTypes =
{
    text: PropTypes.string.isRequired
};

export default PrivatePage;