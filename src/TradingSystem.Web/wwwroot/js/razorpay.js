window.openRazorpayCheckout = function (options, dotnetHelper) {
    var rzp = new Razorpay({
        key: options.keyId,
        amount: options.amount,
        currency: options.currency,
        name: options.companyName,
        description: options.description,
        order_id: options.orderId,
        handler: function (response) {
            dotnetHelper.invokeMethodAsync('OnPaymentSuccess',
                response.razorpay_order_id,
                response.razorpay_payment_id,
                response.razorpay_signature);
        },
        modal: {
            ondismiss: function () {
                dotnetHelper.invokeMethodAsync('OnPaymentCancelled');
            }
        },
        prefill: {
            email: options.email || '',
            contact: options.phone || ''
        },
        theme: {
            color: '#6366f1'
        }
    });
    rzp.on('payment.failed', function (response) {
        dotnetHelper.invokeMethodAsync('OnPaymentFailed', response.error.description);
    });
    rzp.open();
};
