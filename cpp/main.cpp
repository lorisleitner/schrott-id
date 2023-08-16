#define CATCH_CONFIG_MAIN

#include "lib/catch2.hpp"

#include "schrott_id.hpp"

using namespace Catch;
using namespace schrott_id_n;

auto test_permutation = "HwEMFAcAMAYEPxc4Dy4RAxAkEgstJggbGSMiKB0yHgk7OSsNMxoYKRMWNg49LzEFFTQKPDUhHAIsICclOio+Nw==";

TEST_CASE("Encode and decode first 10000")
{
    schrott_id schrott_id(alphabets::base64, test_permutation, 3);

    for (std::uint64_t i = 0; i < 10000; ++i)
    {
        auto encoded = schrott_id.encode(i);
        auto decoded = schrott_id.decode(encoded);

        REQUIRE(decoded == i);
    }
}

TEST_CASE("Test encode decode control")
{
    // control.txt contains the encoded values from 0 to 9999

    std::ifstream control_file("../../test/control.txt");
    REQUIRE_FALSE(control_file.fail());

    std::string line;
    std::vector<std::string> control_lines;
    control_lines.reserve(10000);

    while (std::getline(control_file, line))
    {
        if (!line.empty()
            && line.find('#') != 0)
        {
            control_lines.emplace_back(std::move(line));
        }
    }

    schrott_id schrott_id(alphabets::base64, test_permutation, 3);

    std::vector<std::string> schrott_ids;
    schrott_ids.reserve(10000);

    for (auto i = 0; i < 10000; ++i)
    {
        schrott_ids.emplace_back(schrott_id.encode(i));
    }

    REQUIRE(std::equal(control_lines.begin(), control_lines.end(), schrott_ids.begin()));
}

TEST_CASE("Generate permutation")
{
    // This test depends on randomness, loop 1000 times to make sure we cover as many cases as possible

    for (auto i = 0; i < 1000; ++i)
    {
        auto permutationString = schrott_id::generate_permutation(alphabets::base64);

        auto permutation = base64::decode(permutationString);

        REQUIRE(permutation.size() == strlen(alphabets::base64));
        REQUIRE(util::is_unique(permutation.begin(), permutation.end()));

        int min_element = *std::min_element(permutation.begin(), permutation.end());
        int max_element = *std::max_element(permutation.begin(), permutation.end());

        REQUIRE(min_element == 0);
        REQUIRE(max_element == strlen(alphabets::base64) - 1);
    }
}

TEST_CASE("Generate permutation alphabet too short")
{
    REQUIRE_THROWS_WITH(schrott_id::generate_permutation("A"),
                        Contains("Alphabet must have 2 to 256 characters"));
}

TEST_CASE("Generate permutation alphabet too long")
{
    REQUIRE_THROWS_WITH(schrott_id::generate_permutation("A"),
                        Contains("Alphabet must have 2 to 256 characters"));
}

TEST_CASE("Generate permutation alphabet not unique")
{
    REQUIRE_THROWS_WITH(schrott_id::generate_permutation("A"),
                        Contains("Alphabet must have 2 to 256 characters"));
}

TEST_CASE("Alphabet too short")
{
    REQUIRE_THROWS_WITH(schrott_id::generate_permutation("A"),
                        Contains("Alphabet must have 2 to 256 characters"));
}

TEST_CASE("Alphabet too long")
{
    REQUIRE_THROWS_WITH(schrott_id(std::string(257, 'A'), test_permutation, 3),
                        Contains("Alphabet must have 2 to 256 characters"));
}

TEST_CASE("Alphabet not unique")
{
    REQUIRE_THROWS_WITH(schrott_id(std::string(3, 'A'), test_permutation, 3),
                        Contains("Alphabet must have unique characters"));
}

TEST_CASE("Min length negative")
{
    REQUIRE_THROWS_WITH(schrott_id("ABC", test_permutation, -1),
                        Contains("min_length must be greater than 0"));
}

TEST_CASE("Permutation invalid Base64")
{
    REQUIRE_THROWS_WITH(schrott_id("ABC", "√∫¥", 3),
                        Contains("Base64"));
}

TEST_CASE("Permutation length not equal to alphabet")
{
    REQUIRE_THROWS_WITH(schrott_id(alphabets::base64, "ChwDGxoUBBMLFRARDhIFDAIXGAcAHg0PAR8WCAYdCRk=", 3),
                        Contains("Permutation length must be equal to alphabet length"));
}

TEST_CASE("Permutation not unique")
{
    REQUIRE_THROWS_WITH(schrott_id(alphabets::base32, "QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUE=", 3),
                        Contains("All positions must be unique"));
}

TEST_CASE("Permutation invalid indices")
{
    REQUIRE_THROWS_WITH(schrott_id(alphabets::base32, "twUkTIghtQiRcOQfJtmNRrYbOa9viXe784YeeHp8gec=", 3),
                        Contains("Invalid indices for used alphabet"));
}

TEST_CASE("Decode invalid")
{
    auto s_id = schrott_id(alphabets::base64, test_permutation, 3);

    REQUIRE_THROWS_WITH(s_id.decode("$%&"), Contains("Character not in alphabet"));
}