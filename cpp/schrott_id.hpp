/**
 * Single header implementation for generating SchrottIDs
 */

#ifndef SCHROTT_ID_HPP
#define SCHROTT_ID_HPP

#include <cmath>
#include <map>
#include <random>
#include <string>
#include <vector>

namespace s_id
{
    using byte = std::uint8_t;

    namespace base64
    {
        // https://vorbrodt.blog/2019/03/23/base64-encoding/

        static const char kEncodeLookup[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        static const char kPadCharacter = '=';

        std::string encode(const std::vector<byte>& input)
        {
            std::string encoded;
            encoded.reserve(((input.size() / 3) + (input.size() % 3 > 0)) * 4);

            std::uint32_t temp{};
            auto it = input.begin();

            for (std::size_t i = 0; i < input.size() / 3; ++i)
            {
                temp = (*it++) << 16;
                temp += (*it++) << 8;
                temp += (*it++);
                encoded.append(1, kEncodeLookup[(temp & 0x00FC0000) >> 18]);
                encoded.append(1, kEncodeLookup[(temp & 0x0003F000) >> 12]);
                encoded.append(1, kEncodeLookup[(temp & 0x00000FC0) >> 6]);
                encoded.append(1, kEncodeLookup[(temp & 0x0000003F)]);
            }

            switch (input.size() % 3)
            {
                case 1:
                    temp = (*it++) << 16;
                    encoded.append(1, kEncodeLookup[(temp & 0x00FC0000) >> 18]);
                    encoded.append(1, kEncodeLookup[(temp & 0x0003F000) >> 12]);
                    encoded.append(2, kPadCharacter);
                    break;
                case 2:
                    temp = (*it++) << 16;
                    temp += (*it++) << 8;
                    encoded.append(1, kEncodeLookup[(temp & 0x00FC0000) >> 18]);
                    encoded.append(1, kEncodeLookup[(temp & 0x0003F000) >> 12]);
                    encoded.append(1, kEncodeLookup[(temp & 0x00000FC0) >> 6]);
                    encoded.append(1, kPadCharacter);
                    break;
            }

            return encoded;
        }

        std::vector<byte> decode(const std::string& input)
        {
            if (input.length() % 4)
            {
                throw std::invalid_argument("Invalid Base64 length");
            }

            std::size_t padding{};

            if (input.length())
            {
                if (input[input.length() - 1] == kPadCharacter)
                { padding++; }
                if (input[input.length() - 2] == kPadCharacter)
                { padding++; }
            }

            std::vector<byte> decoded;
            decoded.reserve(((input.length() / 4) * 3) - padding);

            std::uint32_t temp{};
            auto it = input.begin();

            while (it < input.end())
            {
                for (std::size_t i = 0; i < 4; ++i)
                {
                    temp <<= 6;
                    if (*it >= 0x41 && *it <= 0x5A)
                    {
                        temp |= *it - 0x41;
                    }
                    else if (*it >= 0x61 && *it <= 0x7A)
                    {
                        temp |= *it - 0x47;
                    }
                    else if (*it >= 0x30 && *it <= 0x39)
                    {
                        temp |= *it + 0x04;
                    }
                    else if (*it == 0x2B)
                    {
                        temp |= 0x3E;
                    }
                    else if (*it == 0x2F)
                    {
                        temp |= 0x3F;
                    }
                    else if (*it == kPadCharacter)
                    {
                        switch (input.end() - it)
                        {
                            case 1:
                                decoded.push_back((temp >> 16) & 0x000000FF);
                                decoded.push_back((temp >> 8) & 0x000000FF);
                                return decoded;
                            case 2:
                                decoded.push_back((temp >> 10) & 0x000000FF);
                                return decoded;
                            default:
                                throw std::invalid_argument("Invalid padding in Base64");
                        }
                    }
                    else
                    {
                        throw std::invalid_argument("Invalid character in Base64");
                    }

                    ++it;
                }

                decoded.push_back((temp >> 16) & 0x000000FF);
                decoded.push_back((temp >> 8) & 0x000000FF);
                decoded.push_back((temp) & 0x000000FF);
            }

            return decoded;
        }
    }

    namespace util
    {
        /**
         * Tests whether a range of elements only contains unique elements.
         * @tparam It Iterator type
         * @param begin Range begin
         * @param end Range end
         * @return True, if range of elements is unique
         */
        template<class It>
        bool is_unique(It begin, It end)
        {
            std::set<typename It::value_type> set;

            for (auto it = begin; it != end; ++it)
            {
                if (!set.insert(*it).second)
                {
                    return false;
                }
            }

            return true;
        }
    }

    namespace alphabets
    {
        const char* base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        const char* base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        const char* base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const char* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    }

    /**
     * Provides encoding and decoding of SchrottIDs
     */
    class schrott_id
    {
    private:
        std::string alphabet_;
        std::map<char, byte> inverse_alphabet_;

        std::vector<byte> permutation_;
        std::vector<byte> inverse_permutation_;

        int min_length_;

    public:

        /**
         * Creates a new instance of the SchrottID encoder class.
         *
         * SchrottIDs can only be decoded if the parameters to this constructor are equal
         * to the ones that were supplied to create the SchrottID.
         *
         * This constructor verifies parameters and creates internal structures.
         * Instances should be reused as often as possible.
         * @param alphabet The alphabet that the encoder and decoder will use.
         * @param permutation The randomly generated permutation to use.
         * Generate permutations using @see generate_permutation
         * Permutations are dependent on the supplied alphabet.
         * @param min_length The minimum length of the encoded ID that the @see encode method will produce.
         * @throws std::invalid_argument A supplied parameter cannot be used to create an encoder.
         */
        schrott_id(
                std::string alphabet,
                const std::string& permutation,
                int min_length)
                : alphabet_(std::move(alphabet)),
                  min_length_(min_length)
        {
            if (alphabet_.size() <= 1
                || alphabet_.size() > 256)
            {
                throw std::invalid_argument("Alphabet must have 2 to 256 characters");
            }

            if (!util::is_unique(alphabet_.begin(), alphabet_.end()))
            {
                throw std::invalid_argument("Alphabet must have unique characters");
            }

            if (min_length_ <= 0)
            {
                throw std::invalid_argument("min_length must be greater than 0");
            }

            for (auto i = 0; i < alphabet_.size(); ++i)
            {
                inverse_alphabet_[alphabet_[i]] = i;
            }

            permutation_ = base64::decode(permutation);

            if (permutation_.size() != alphabet_.size())
            {
                throw std::invalid_argument("Permutation length must be equal to alphabet length. "
                                            "Please make sure to use a valid permutation for this alphabet");
            }

            if (!util::is_unique(permutation_.begin(), permutation_.end()))
            {
                throw std::invalid_argument("Invalid permutation. All positions must be unique.");
            }

            if (*std::min_element(permutation_.begin(), permutation_.end()) != 0
                || *std::max_element(permutation_.begin(), permutation_.end()) != alphabet_.size() - 1)
            {
                throw std::invalid_argument("Invalid permutation. Invalid indices for used alphabet.");
            }

            inverse_permutation_.resize(permutation_.size());
            for (auto i = 0; i < permutation_.size(); ++i)
            {
                inverse_permutation_[permutation_[i]] = i;
            }
        }

        /**
         * Generates a secure random permutation for the supplied alphabet.
         * @param alphabet The alphabet
         * @return A randomly generated permutation to use with the @see schrott_id class
         * @throws std::invald_argument Alphabet is not between 2 and 256 chars long or chars are not unique.
         */
        static std::string generate_permutation(const std::string& alphabet)
        {
            if (alphabet.size() <= 1
                || alphabet.size() > 256)
            {
                throw std::invalid_argument("Alphabet must have 2 to 256 characters");
            }

            if (!util::is_unique(alphabet.begin(), alphabet.end()))
            {
                throw std::invalid_argument("Alphabet must have unique characters");
            }

            std::random_device rd;
            std::uniform_int_distribution<int> distribution(0, alphabet.size() - 1);

            std::vector<byte> permutation(alphabet.size());

            for (auto i = 0; i < permutation.size(); ++i)
            {
                permutation[i] = i;
            }

            for (auto i = 0; i < permutation.size(); ++i)
            {
                auto p = distribution(rd);
                std::swap(permutation[i], permutation[p]);
            }

            return base64::encode(permutation);
        }

        /**
         * Encodes an integer value to a SchrottID
         * @param value The value to encode
         * @return Encoded SchrottID
         */
        std::string encode(std::uint64_t value) const
        {
            auto buf = convert_to_base(value);

            for (auto i = 0; i < buf.size() * 3; ++i)
            {
                rotate_left(buf);
                permute_forward(buf);
                rotate_left(buf);
                cascade_forward(buf);
                rotate_left(buf);
            }

            return convert_to_string(buf);
        }

        /**
         * Decodes a SchrottID back to an integer value
         * @param value The value to decode
         * @return The decoded SchrottID
         * @throws std::out_of_range The supplied value contains a character that is not present in the alphabet.
         */
        std::uint64_t decode(const std::string& value) const
        {
            auto buf = convert_from_base(value);

            for (auto i = 0; i < buf.size() * 3; ++i)
            {
                rotate_right(buf);
                cascade_backward(buf);
                rotate_right(buf);
                permute_backward(buf);
                rotate_right(buf);
            }

            return convert_to_value(buf);
        }

    private:

        std::vector<byte> convert_to_base(std::uint64_t value) const
        {
            auto len = std::max(
                    static_cast<int>(std::ceil(
                            (std::log(value + 1) / std::log(alphabet_.size())))),
                    min_length_);

            std::vector<byte> buf(len);

            auto i = buf.size();
            do
            {
                buf[--i] = value % alphabet_.size();
                value = value / alphabet_.size();
            } while (value > 0);

            return buf;
        }

        std::string convert_to_string(const std::vector<byte>& buf) const
        {
            std::string s;
            s.resize(buf.size());

            for (auto i = 0; i < buf.size(); ++i)
            {
                s[i] = alphabet_[buf[i]];
            }

            return s;
        }

        std::vector<byte> convert_from_base(const std::string& value) const
        {
            std::vector<byte> buf(value.size());

            for (auto i = 0; i < buf.size(); ++i)
            {
                auto elem = inverse_alphabet_.find(value[i]);

                if (elem != inverse_alphabet_.end())
                {
                    buf[i] = elem->second;
                }
                else
                {
                    throw std::out_of_range("Character not in alphabet");
                }
            }

            return buf;
        }

        std::uint64_t convert_to_value(const std::vector<byte>& buf) const
        {
            std::uint64_t value = 0;

            for (auto i = 0; i < buf.size(); ++i)
            {
                if (i > 0)
                {
                    value *= alphabet_.size();
                }

                value += buf[i];
            }

            return value;
        }

        void permute_forward(std::vector<byte>& buf) const
        {
            for (auto i = 0; i < buf.size(); ++i)
            {
                buf[i] = permutation_[buf[i]];
            }
        }

        void permute_backward(std::vector<byte>& buf) const
        {
            for (auto i = 0; i < buf.size(); ++i)
            {
                buf[i] = inverse_permutation_[buf[i]];
            }
        }

        void rotate_left(std::vector<byte>& buf) const
        {
            std::rotate(buf.begin(), buf.begin() + 1, buf.end());
        }

        void rotate_right(std::vector<byte>& buf) const
        {
            std::rotate(buf.rbegin(), buf.rbegin() + 1, buf.rend());
        }

        void cascade_forward(std::vector<byte>& buf) const
        {
            byte last = 0;

            for (auto& i: buf)
            {
                i = (i + last) % alphabet_.size();
                last = i;
            }
        }

        void cascade_backward(std::vector<byte>& buf) const
        {
            byte last = 0;

            for (auto i = 0; i < buf.size(); ++i)
            {
                auto t = buf[i];
                buf[i] = (buf[i] + alphabet_.size() - last) % alphabet_.size();
                last = t;
            }
        }
    };
}

#endif // SCHROTT_ID_HPP
