﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WxPayAPI;
using Xiaowei.Model;

namespace Xiaowei.API
{
    public class XiaoWei
    {

        /// <summary>
        /// 检查必填参数
        /// </summary>
        /// <param name="inputObj"></param>
        private static void CheckParams(WxPayData inputObj)
        {
            string[] datas =
            {
                 "id_card_copy", "id_card_national", "id_card_name", "id_card_number", "id_card_valid_time", "account_name", "account_bank", "bank_address_code", "account_number", "store_name", "store_address_code", "store_street", "store_entrance_pic", "indoor_pic", "merchant_shortname", "service_phone", "business", "contact", "contact_phone"
             };

            foreach (string s in datas)
            {
                if (!inputObj.IsSet(s))
                {
                    throw new WxPayException($"缺少必填参数: {s}！");
                }
            }

        }

        /// <summary>
        /// 银行卡号检查
        /// </summary>
        /// <param name="account_number"></param>
        private static void AccountNumberIsSupport(string account_number)
        {
            string account_prefix_6 = account_number.Substring(0, 6);
            string account_prefix_8 = account_number.Substring(0, 8);
            string[] not_support =
            {
                "623501", "621468", "620522", "625191", "622384", "623078", "940034", "622150", "622151", "622181", "622188", "955100", "621095", "620062", "621285", "621798", "621799", "621797", "622199", "621096", "62215049", "62215050", "62215051", "62218849", "62218850", "62218851", "621622", "623219", "621674", "623218", "621599", "623698", "623699", "623686", "621098", "620529", "622180", "622182", "622187", "622189", "621582", "623676", "623677", "622812", "622810", "622811", "628310", "625919", "625368", "625367", "518905", "622835", "625603", "625605", "518905"
            };


            if (not_support.ToList().IndexOf(account_prefix_6) == 0)
            {
                throw new WxPayException($"不支持的银行卡号！");
            }

            if (not_support.ToList().IndexOf(account_prefix_8) == 0)
            {
                throw new WxPayException($"不支持的银行卡号！");
            }
        }

        /// <summary>
        /// 敏感信息加密
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string InfoEncryption(string s)
        {
            string ciphertext = Certificates.Get().data.FirstOrDefault().encrypt_certificate.ciphertext;
            string associated_data = Certificates.Get().data.FirstOrDefault().encrypt_certificate.associated_data;
            string nonce_dc = Certificates.Get().data.FirstOrDefault().encrypt_certificate.nonce;

            //通过处理数据加密,暂时通过php7实现
            string url = "http://localhost/info.php";
            MsMultiPartFormData form = new MsMultiPartFormData();
            form.AddFormField("ciphertext", ciphertext);
            form.AddFormField("associated_data", associated_data);
            form.AddFormField("nonce", nonce_dc);
            form.AddFormField("string", s);//签名

            string rst = Lib.HttpService.Post("", url, form, true, 10);
            return rst;
        }

        /// <summary>
        /// 提交资料
        /// </summary>
        /// <param name="inputObj"></param>
        /// <returns></returns>
        public static string Submit(WxPayData inputObj)
        {
            //WxPayData inputObj = new WxPayData();
            inputObj.SetValue("version", "2.0");//接口版本号
            inputObj.SetValue("mch_id", Lib.Config.MchId);//商户号
            inputObj.SetValue("nonce_str", WxPayApi.GenerateNonceStr());//随机字符串

            //检测必填参数
            CheckParams(inputObj);
            AccountNumberIsSupport(inputObj.GetValue("account_number").ToString());


            ////敏感信息加密
            inputObj.SetValue("cert_sn", Certificates.Get().data.FirstOrDefault().serial_no);//平台证书序列号
            inputObj.SetValue("id_card_name", InfoEncryption(inputObj.GetValue("id_card_name").ToString()));//身份证姓名
            inputObj.SetValue("id_card_number", InfoEncryption(inputObj.GetValue("id_card_number").ToString()));//身份证
            inputObj.SetValue("account_name", InfoEncryption(inputObj.GetValue("account_name").ToString()));//开户名称
            inputObj.SetValue("account_number", InfoEncryption(inputObj.GetValue("account_number").ToString()));//银行账号
            inputObj.SetValue("contact", InfoEncryption(inputObj.GetValue("contact").ToString()));//联系人姓名
            inputObj.SetValue("contact_phone", InfoEncryption(inputObj.GetValue("contact_phone").ToString()));//手机号码


            inputObj.SetValue("sign_type", WxPayData.SIGN_TYPE_HMAC_SHA256);//签名类型
            inputObj.SetValue("sign", inputObj.MakeSign());//签名

            string xml = inputObj.ToXml();
            string url = "https://api.mch.weixin.qq.com/applyment/micro/submit";
            int timeOut = 10;

            Log.Debug("申请入驻", "request : " + xml);

            //调用HTTP通信接口以提交数据到API
            string response = HttpService.Post(xml, url, true, timeOut);

            Log.Debug("申请入驻", "response : " + response);

            return response;

        }

        /// <summary>
        /// 获取审核结果
        /// </summary>
        /// <param name="business_code"></param>
        /// <returns></returns>
        public static string Getstate(string business_code)

        {
            WxPayData inputObj = new WxPayData();

            inputObj.SetValue("version", "1.0");//接口版本号
            inputObj.SetValue("mch_id", WxPayConfig.GetConfig().GetMchID());//商户号
            inputObj.SetValue("nonce_str", WxPayApi.GenerateNonceStr());//随机字符串
            inputObj.SetValue("business_code", business_code);//

            inputObj.SetValue("sign_type", WxPayData.SIGN_TYPE_HMAC_SHA256);//签名类型
            inputObj.SetValue("sign", inputObj.MakeSign());//签名

            string xml = inputObj.ToXml();
            string url = "https://api.mch.weixin.qq.com/applyment/micro/getstate";
            int timeOut = 10;

            Log.Debug("申请入驻", "request : " + xml);
            string response = HttpService.Post(xml, url, true, timeOut);//调用HTTP通信接口以提交数据到API
            Log.Debug("申请入驻", "response : " + response);

            return response;

        }
    }
}
